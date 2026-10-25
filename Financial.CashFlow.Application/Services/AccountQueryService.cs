using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class AccountQueryService : IAccountQueryService
{
    private const string EntityType = "Account";

    private enum TransactionKind
    {
        Expense,
        CardCharge,
        Income,
        TransferIn,
        TransferOut,
        Adjustment
    }

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AccountQueryService> _logger;

    public AccountQueryService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<AccountQueryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<AccountDTO> GetAccounts()
    {
        using var span = StartSpan("GetAccounts");
        try
        {
            var referencedIds = AccountReferenceReader.ReferencedAccountIds(_repository);
            var result = _repository.GetAccounts()
                .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
                .Select(account => ToAccountDto(account, referencedIds.Contains(account.Id)))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAccounts");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public AccountDetailDTO GetAccount(Guid id, int year, int month)
    {
        using var span = StartSpan("GetAccount");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var endOfMonth = EndOfMonth(year, month);
            var account = FindAccountOrThrow(id);
            var records = AccountTransactionReader.Read(_repository);

            var result = new AccountDetailDTO
            {
                Id = account.Id,
                Name = account.Name,
                TemplateName = account.TemplateName,
                Currency = account.Currency.ToString(),
                IsActive = account.IsActive,
                Year = year,
                Month = month,
                Balance = AccountBalanceRule.Balance(account, endOfMonth, records),
                BalanceIsOwed = account.Has(Capability.HasStatements),
                AllowedActions = AccountActions.AllowedFor(account).Select(action => action.ToString()).ToList()
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAccount");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<AccountTransactionDTO> GetTransactions(Guid id, int year, int month)
    {
        using var span = StartSpan("GetAccountTransactions");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var endOfMonth = EndOfMonth(year, month);
            var startOfMonth = new DateOnly(year, month, 1);
            var account = FindAccountOrThrow(id);

            var result = AccountTransactionReader.Read(_repository)
                .Where(record => record.Date >= startOfMonth && record.Date <= endOfMonth)
                .SelectMany(record => ToRows(record, account))
                .OrderByDescending(row => row.Date)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAccountTransactions");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static IEnumerable<AccountTransactionDTO> ToRows(IAccountTransaction record, Account account) =>
        record switch
        {
            Expense expense when expense.PaymentSourceAccount?.Id == account.Id || expense.IsUnpaidChargeOn(account) => [ToRow(expense, account)],
            Income income when income.Account?.Id == account.Id => [ToRow(income, account)],
            Transfer transfer => ToRows(transfer, account),
            BalanceAdjustment adjustment when adjustment.Account.Id == account.Id => [ToRow(adjustment, account)],
            _ => []
        };

    private static AccountTransactionDTO ToRow(Expense expense, Account account)
    {
        var kind = expense.CreditCard is null ? TransactionKind.Expense : TransactionKind.CardCharge;
        return new AccountTransactionDTO
        {
            Id = expense.Id,
            Kind = kind.ToString(),
            Date = expense.Date,
            Description = expense.Description,
            CategoryName = expense.Category.Name,
            Counterpart = expense.CreditCard?.Id == account.Id ? null : expense.CreditCard?.Name,
            Amount = -expense.Value,
            RoundUp = expense.RoundUpAmount is { } roundUp ? -roundUp : null,
            CanEdit = account.IsActive && kind == TransactionKind.Expense,
            Expense = ExpenseService.ToDto(expense)
        };
    }

    private static AccountTransactionDTO ToRow(Income income, Account account) => new()
    {
        Id = income.Id,
        Kind = TransactionKind.Income.ToString(),
        Date = income.Date,
        Description = income.Description,
        Counterpart = income.IncomeSource.Name,
        Amount = income.NetValue,
        CanEdit = account.IsActive,
        Income = IncomeService.ToDto(income)
    };

    private static IEnumerable<AccountTransactionDTO> ToRows(Transfer transfer, Account account)
    {
        if (transfer.SourceAccount.Id == account.Id)
            yield return ToRow(transfer, account, TransactionKind.TransferOut, transfer.DestinationAccount, -transfer.Amount);

        if (transfer.DestinationAccount.Id == account.Id)
            yield return ToRow(transfer, account, TransactionKind.TransferIn, transfer.SourceAccount, transfer.AmountReceived());
    }

    private static AccountTransactionDTO ToRow(Transfer transfer, Account account, TransactionKind kind, Account counterpart, decimal amount) => new()
    {
        Id = transfer.Id,
        Kind = kind.ToString(),
        Date = transfer.Date,
        Description = transfer.Note,
        Counterpart = counterpart.Name,
        Amount = amount,
        CanEdit = account.IsActive,
        Transfer = AccountTransferService.ToDto(transfer)
    };

    private static AccountTransactionDTO ToRow(BalanceAdjustment adjustment, Account account) => new()
    {
        Id = adjustment.Id,
        Kind = TransactionKind.Adjustment.ToString(),
        Date = adjustment.Date,
        Description = adjustment.Note,
        Amount = adjustment.Delta,
        CanEdit = account.IsActive,
        Adjustment = BalanceAdjustmentService.ToDto(adjustment)
    };

    internal static AccountDTO ToAccountDto(Account account, bool hasRecords) => new()
    {
        Id = account.Id,
        Name = account.Name,
        TemplateName = account.TemplateName,
        Currency = account.Currency.ToString(),
        IsActive = account.IsActive,
        HasRecords = hasRecords,
        OpeningBalance = account.OpeningBalance?.Amount,
        BalanceStartDate = account.OpeningBalance?.StartDate,
        RoundUpEnabled = account.RoundUp?.Enabled
    };

    private static DateOnly EndOfMonth(int year, int month) => new(year, month, DateTime.DaysInMonth(year, month));

    private Account FindAccountOrThrow(Guid id) =>
        _repository.GetAccounts().FirstOrThrow(account => account.Id == id, EntityType, id);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(AccountQueryService), operationName, EntityType);
    }
}
