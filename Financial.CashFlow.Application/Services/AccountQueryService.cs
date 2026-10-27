using System.Globalization;
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
        CardPayment,
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
                BalanceIsOwed = account.BalanceIsOwed,
                HasStatements = account.Has(Capability.HasStatements),
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
            var account = FindAccountOrThrow(id);
            var statements = _repository.GetCardStatements().ToList();
            var rows = account.Has(Capability.HasStatements)
                ? CardRows(account, year, month, statements)
                : AccountRows(account, year, month, statements);

            var result = rows.OrderByDescending(row => row.Date).ToList();

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

    public IReadOnlyList<AccountStatementDTO> GetStatements(Guid id)
    {
        using var span = StartSpan("GetAccountStatements");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var card = FindAccountOrThrow(id);
            CardAccountRule.EnsureIsCard(card);
            var expenses = _repository.GetExpenses().ToList();
            var transfers = _repository.GetTransfers().ToList();

            var result = _repository.GetCardStatements()
                .Where(statement => statement.CreditCard.Id == card.Id)
                .Select(statement => (Statement: statement, Charges: CardChargeFilter.ChargesFor(expenses, card, statement.Year, statement.Month).ToList()))
                .Where(entry => entry.Statement.IsPaid || entry.Charges.Count > 0)
                .OrderByDescending(entry => entry.Statement.Year)
                .ThenByDescending(entry => entry.Statement.Month)
                .Select(entry => ToStatementDto(entry.Statement, entry.Charges, transfers))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAccountStatements");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<PayableStatementDTO> GetPayableStatements()
    {
        using var span = StartSpan("GetPayableStatements");
        try
        {
            var expenses = _repository.GetExpenses().ToList();

            var result = _repository.GetCardStatements()
                .Where(statement => !statement.IsPaid)
                .Select(statement => ToPayableDto(statement, CardChargeFilter.OutstandingTotal(expenses, [statement], statement)))
                .Where(statement => statement.Total > 0)
                .OrderBy(statement => statement.CreditCardName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(statement => statement.Year)
                .ThenBy(statement => statement.Month)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetPayableStatements");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static PayableStatementDTO ToPayableDto(CardStatement statement, decimal total) => new()
    {
        Id = statement.Id,
        CreditCardId = statement.CreditCard.Id,
        CreditCardName = statement.CreditCard.Name,
        Year = statement.Year,
        Month = statement.Month,
        Total = total,
        Currency = statement.CreditCard.Currency.ToString()
    };

    private static AccountStatementDTO ToStatementDto(CardStatement statement, IReadOnlyList<Expense> charges, IReadOnlyList<Transfer> transfers)
    {
        var payment = PaymentOf(statement, charges, transfers);
        return new AccountStatementDTO
        {
            Id = statement.Id,
            Year = statement.Year,
            Month = statement.Month,
            Total = charges.Sum(charge => charge.Value),
            OutstandingTotal = CardChargeFilter.OutstandingTotal(charges, [statement], statement),
            IsPaid = statement.IsPaid,
            DueDate = statement.DueDate,
            PaidOn = payment?.Date,
            PaidFromAccountName = payment?.Account.Name
        };
    }

    private static (DateOnly Date, Account Account)? PaymentOf(CardStatement statement, IReadOnlyList<Expense> charges, IReadOnlyList<Transfer> transfers)
    {
        if (!statement.IsPaid)
            return null;

        if (statement.IsPaidByTransfer)
            return transfers.FirstOrDefault(transfer => transfer.Id == statement.PaymentTransferId) is { } transfer
                ? (transfer.Date, transfer.SourceAccount)
                : null;

        return CardChargeFilter.SettledChargesFor(charges, statement.CreditCard, statement.Year, statement.Month)
            .MaxBy(charge => charge.Date) is { PaymentSourceAccount: { } bank } latest
            ? (latest.Date, bank)
            : null;
    }

    private IEnumerable<AccountTransactionDTO> AccountRows(Account account, int year, int month, IReadOnlyList<CardStatement> statements) =>
        RecordsDatedIn(year, month).SelectMany(record => ToRows(record, account, statements));

    private IEnumerable<AccountTransactionDTO> CardRows(Account card, int year, int month, IReadOnlyList<CardStatement> statements) =>
        RecordsDatedIn(year, month)
            .Where(record => record is not Expense)
            .SelectMany(record => ToRows(record, card, statements))
            .Concat(CardChargeRows(card, year, month, statements));

    private IEnumerable<IAccountTransaction> RecordsDatedIn(int year, int month)
    {
        var startOfMonth = new DateOnly(year, month, 1);
        var endOfMonth = EndOfMonth(year, month);
        return AccountTransactionReader.Read(_repository).Where(record => record.Date >= startOfMonth && record.Date <= endOfMonth);
    }

    private IEnumerable<AccountTransactionDTO> CardChargeRows(Account card, int year, int month, IReadOnlyList<CardStatement> statements) =>
        CardChargeFilter.ChargesFor(_repository.GetExpenses(), card, year, month)
            .Select(charge => ToRow(
                charge,
                TransactionKind.CardCharge,
                charge.ChargeDate ?? charge.Date,
                charge.PaymentSourceAccount?.Name,
                roundUp: null,
                canEdit: card.IsActive && !CardChargeFilter.IsPaid(charge, statements),
                statements));

    private static IEnumerable<AccountTransactionDTO> ToRows(IAccountTransaction record, Account account, IReadOnlyList<CardStatement> statements) =>
        record switch
        {
            Expense expense when expense.PaymentSourceAccount?.Id == account.Id => [ToRow(expense, account, statements)],
            Income income when income.Account?.Id == account.Id => [ToRow(income, account)],
            Transfer transfer => ToRows(transfer, account, statements),
            BalanceAdjustment adjustment when adjustment.Account.Id == account.Id => [ToRow(adjustment, account)],
            _ => []
        };

    private static AccountTransactionDTO ToRow(Expense expense, Account account, IReadOnlyList<CardStatement> statements)
    {
        var kind = expense.CreditCard is null ? TransactionKind.Expense : TransactionKind.CardCharge;
        return ToRow(
            expense,
            kind,
            expense.Date,
            expense.CreditCard?.Name,
            expense.RoundUpAmount is { } roundUp ? -roundUp : null,
            account.IsActive && kind == TransactionKind.Expense,
            statements);
    }

    private static AccountTransactionDTO ToRow(
        Expense expense, TransactionKind kind, DateOnly date, string? counterpart, decimal? roundUp, bool canEdit, IReadOnlyList<CardStatement> statements) =>
        new()
        {
            Id = expense.Id,
            Kind = kind.ToString(),
            Date = date,
            Description = expense.Description,
            CategoryName = expense.Category.Name,
            Counterpart = counterpart,
            Amount = -expense.Value,
            RoundUp = roundUp,
            CanEdit = canEdit,
            Expense = ExpenseService.ToDto(expense, statements)
        };

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

    private static IEnumerable<AccountTransactionDTO> ToRows(Transfer transfer, Account account, IReadOnlyList<CardStatement> statements)
    {
        var paidStatement = transfer.IsStatementPayment
            ? statements.FirstOrDefault(statement => statement.PaymentTransferId == transfer.Id)
            : null;

        if (transfer.SourceAccount.Id == account.Id)
        {
            var kind = transfer.IsStatementPayment ? TransactionKind.CardPayment : TransactionKind.TransferOut;
            yield return ToRow(transfer, account, kind, transfer.DestinationAccount, -transfer.Amount, paidStatement);
        }

        if (transfer.DestinationAccount.Id == account.Id)
        {
            var kind = transfer.IsStatementPayment ? TransactionKind.CardPayment : TransactionKind.TransferIn;
            yield return ToRow(transfer, account, kind, transfer.SourceAccount, transfer.AmountReceived(), paidStatement);
        }
    }

    private static AccountTransactionDTO ToRow(
        Transfer transfer, Account account, TransactionKind kind, Account counterpart, decimal amount, CardStatement? paidStatement) => new()
    {
        Id = transfer.Id,
        Kind = kind.ToString(),
        Date = transfer.Date,
        Description = paidStatement is null ? transfer.Note : PaymentDescription(paidStatement),
        Counterpart = counterpart.Name,
        Amount = amount,
        CanEdit = account.IsActive && !transfer.IsStatementPayment,
        Statement = paidStatement is null ? null : ToStatementLink(paidStatement),
        Transfer = AccountTransferService.ToDto(transfer)
    };

    private static string PaymentDescription(CardStatement statement) =>
        $"{statement.CreditCard.Name} payment – {new DateOnly(statement.Year, statement.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture)} statement";

    private static AccountTransactionStatementDTO ToStatementLink(CardStatement statement) => new()
    {
        AccountId = statement.CreditCard.Id,
        Year = statement.Year,
        Month = statement.Month
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
