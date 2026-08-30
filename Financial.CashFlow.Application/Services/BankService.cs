using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
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
            var result = _repository.GetBanks().Select(ToDto).ToList();

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

            EnsureNameIsUnique(request.Name, excludingId: null);

            var bank = Bank.Create(request.Name, request.RoundUpEnabled);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddBank(bank);
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

            if (!EntityIdResolver.TryResolve(id, _repository.GetBanks(), b => b.Id, out var bank))
            {
                throw new KeyNotFoundException($"Bank '{id}' was not found.");
            }

            EnsureNameIsUnique(request.Name, excludingId: id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                bank!.Update(request.Name, request.RoundUpEnabled);
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
            if (!EntityIdResolver.TryResolve(id, _repository.GetBanks(), b => b.Id, out _))
            {
                throw new KeyNotFoundException($"Bank '{id}' was not found.");
            }

            EnsureNotReferenced(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteBank(id);
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

            if (!EntityIdResolver.TryResolve(id, _repository.GetBanks(), b => b.Id, out var bank))
            {
                throw new KeyNotFoundException($"Bank '{id}' was not found.");
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                bank!.SetOpeningBalance(request.OpeningBalance, request.OpeningBalanceDate);
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
            var incomes = _repository.GetIncomes().ToList();
            var expenses = _repository.GetExpenses().ToList();
            var transfers = _repository.GetTransfers().ToList();
            var adjustments = _repository.GetBalanceAdjustments().ToList();

            var result = _repository.GetBanks()
                .Select(bank => new BankBalanceDTO
                {
                    Bank = bank.Name,
                    Balance = ComputeBalance(bank, endOfMonth, incomes, expenses, transfers, adjustments, excludingAdjustmentId: null)
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
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
            {
                throw new KeyNotFoundException($"Bank '{bankId}' was not found.");
            }

            var result = ComputeBalance(
                bank!,
                asOfDate,
                _repository.GetIncomes(),
                _repository.GetExpenses(),
                _repository.GetTransfers(),
                _repository.GetBalanceAdjustments(),
                excludingAdjustmentId);

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

    private void EnsureNameIsUnique(string name, Guid? excludingId)
    {
        var collision = _repository.GetBanks().FirstOrDefault(b => b.Name == name && b.Id != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"A bank named \"{name}\" already exists.");
        }
    }

    /// <summary>
    /// Scans the same four collections <see cref="ComputeBalance"/> already reads (Income, Expense,
    /// Transfer, BalanceAdjustment) - every relationship that can hold a reference to a Bank.
    /// </summary>
    private void EnsureNotReferenced(Guid bankId)
    {
        var referenced =
            _repository.GetBalanceAdjustments().Any(a => a.Bank.Id == bankId) ||
            _repository.GetIncomes().Any(i => i.Bank?.Id == bankId) ||
            _repository.GetExpenses().Any(e => e.PaymentSourceBank?.Id == bankId) ||
            _repository.GetTransfers().Any(t => t.SourceBank.Id == bankId || t.DestinationBank.Id == bankId);

        if (referenced)
        {
            throw new EntityInUseException("Cannot delete a bank that still has balance history or transactions.");
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(BankService), operationName, EntityType);
    }

    private static decimal ComputeBalance(
        Bank bank,
        DateOnly asOfDate,
        IEnumerable<Income> incomes,
        IEnumerable<Expense> expenses,
        IEnumerable<Transfer> transfers,
        IEnumerable<BalanceAdjustment> adjustments,
        Guid? excludingAdjustmentId)
    {
        bool InWindow(DateOnly date) => date >= bank.OpeningBalanceDate && date <= asOfDate;

        var incomeTotal = incomes
            .Where(i => i.Bank?.Id == bank.Id && InWindow(i.Date))
            .Sum(i => i.NetValue);

        var expenseTotal = expenses
            .Where(e => e.PaymentSourceBank?.Id == bank.Id && InWindow(e.Date))
            .Sum(e => e.Value + (e.RoundUpAmount ?? 0));

        var transferInTotal = transfers
            .Where(t => t.DestinationBank.Id == bank.Id && InWindow(t.Date))
            .Sum(t => t.Amount);

        var transferOutTotal = transfers
            .Where(t => t.SourceBank.Id == bank.Id && InWindow(t.Date))
            .Sum(t => t.Amount);

        var adjustmentTotal = adjustments
            .Where(a => a.Bank.Id == bank.Id && InWindow(a.Date) && a.Id != excludingAdjustmentId)
            .Sum(a => a.Delta);

        return bank.OpeningBalance + incomeTotal - expenseTotal + transferInTotal - transferOutTotal + adjustmentTotal;
    }

    private static BankDTO ToDto(Bank bank) => new()
    {
        Id = bank.Id,
        Name = bank.Name,
        RoundUpEnabled = bank.RoundUpEnabled,
        OpeningBalance = bank.OpeningBalance,
        OpeningBalanceDate = bank.OpeningBalanceDate
    };
}
