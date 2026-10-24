using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class BankService : IBankService
{
    private const string EntityType = "Bank";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<BankService> _logger;

    public BankService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<BankService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<BankDTO> GetBanks()
    {
        using var span = StartSpan("GetBanks");
        try
        {
            var referencedAccountIds = BuildReferencedAccountIds();
            var result = BankAccounts().Select(bank => ToDto(bank, referencedAccountIds.Contains(bank.Id))).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBanks");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<BankDTO> CreateBankAsync(BankCreateDTO request)
    {
        using var span = StartSpan("CreateBank");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Bank name is required.", nameof(request));
            }

            _repository.GetAccounts().EnsureNameIsUnique(request.Name.Trim(), null, a => a.Name, a => a.Id, "A bank", StringComparer.OrdinalIgnoreCase);

            var bank = Account.Create(request.Name, AccountTemplate.Bank, Currency.GBP);
            bank.SetRoundUp(request.RoundUpEnabled);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddAccount(bank);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, bank.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateBank");
            return ToDto(bank);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<BankDTO> UpdateBankAsync(Guid id, BankUpdateDTO request)
    {
        using var span = StartSpan("UpdateBank");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Bank name is required.", nameof(request));
            }

            var bank = FindBankOrThrow(id);

            _repository.GetAccounts().EnsureNameIsUnique(request.Name.Trim(), id, a => a.Name, a => a.Id, "A bank", StringComparer.OrdinalIgnoreCase);

            await _repository.ApplyAndSaveAsync(() =>
            {
                bank.Update(request.Name);
                bank.SetRoundUp(request.RoundUpEnabled);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateBank");
            return ToDto(bank);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteBankAsync(Guid id)
    {
        using var span = StartSpan("DeleteBank");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            _ = FindBankOrThrow(id);

            EnsureNotReferenced(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteAccount(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteBank");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request)
    {
        using var span = StartSpan("UpdateOpeningBalance");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var bank = FindBankOrThrow(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                bank.SetOpeningBalance(request.OpeningBalance, request.OpeningBalanceDate);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateOpeningBalance");
            return ToDto(bank);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month)
    {
        using var span = StartSpan("GetBankBalancesByMonth");
        try
        {
            var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var records = ReadAccountTransactions(excludingAdjustmentId: null);

            var result = BankAccounts()
                .Select(bank => new BankBalanceDTO
                {
                    Bank = bank.Name,
                    Balance = AccountBalanceRule.Balance(bank, endOfMonth, records)
                })
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBankBalancesByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null)
    {
        using var span = StartSpan("GetBankBalanceAsOf");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, bankId.ToString());
        try
        {
            var bank = FindBankOrThrow(bankId);

            var result = AccountBalanceRule.Balance(bank, asOfDate, ReadAccountTransactions(excludingAdjustmentId));

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBankBalanceAsOf");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private IEnumerable<Account> BankAccounts() =>
        _repository.GetAccounts().Where(account => account.Template == AccountTemplate.Bank);

    private Account FindBankOrThrow(Guid id) => BankAccounts().FirstOrThrow(b => b.Id == id, "Bank", id);

    private List<IAccountTransaction> ReadAccountTransactions(Guid? excludingAdjustmentId) =>
    [
        .. _repository.GetIncomes(),
        .. _repository.GetExpenses(),
        .. _repository.GetTransfers(),
        .. _repository.GetBalanceAdjustments().Where(a => a.Id != excludingAdjustmentId)
    ];

    private void EnsureNotReferenced(Guid accountId)
    {
        if (IsReferenced(accountId))
        {
            throw new EntityInUseException("Cannot delete a bank that still has balance history or transactions.");
        }
    }

    private bool IsReferenced(Guid accountId) =>
        _repository.GetBalanceAdjustments().Any(a => a.Account.Id == accountId) ||
        _repository.GetIncomes().Any(i => i.Account?.Id == accountId) ||
        _repository.GetExpenses().Any(e => e.PaymentSourceAccount?.Id == accountId) ||
        _repository.GetTransfers().Any(t => t.SourceAccount.Id == accountId || t.DestinationAccount.Id == accountId);

    private HashSet<Guid> BuildReferencedAccountIds()
    {
        var ids = new HashSet<Guid>();
        foreach (var adjustment in _repository.GetBalanceAdjustments()) ids.Add(adjustment.Account.Id);
        foreach (var income in _repository.GetIncomes()) if (income.Account is not null) ids.Add(income.Account.Id);
        foreach (var expense in _repository.GetExpenses()) if (expense.PaymentSourceAccount is not null) ids.Add(expense.PaymentSourceAccount.Id);
        foreach (var transfer in _repository.GetTransfers())
        {
            ids.Add(transfer.SourceAccount.Id);
            ids.Add(transfer.DestinationAccount.Id);
        }
        return ids;
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(BankService), operationName, EntityType);
    }

    private BankDTO ToDto(Account bank) => ToDto(bank, IsReferenced(bank.Id));

    private static BankDTO ToDto(Account bank, bool hasReferences) => new()
    {
        Id = bank.Id,
        Name = bank.Name,
        RoundUpEnabled = bank.RoundsUp,
        OpeningBalance = bank.OpeningBalance?.Amount ?? 0m,
        OpeningBalanceDate = bank.OpeningBalance?.StartDate ?? default,
        HasReferences = hasReferences
    };
}
