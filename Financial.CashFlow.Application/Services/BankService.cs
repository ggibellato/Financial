using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;

namespace Financial.CashFlow.Application.Services;

public sealed class BankService : IBankService
{
    private const string EntityType = "Bank";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public BankService(ICashFlowRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public IReadOnlyList<BankDTO> GetBanks()
    {
        using var span = StartSpan("GetBanks");
        try
        {
            var result = _repository.GetBanks().Select(ToDto).ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
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

            bank!.SetOpeningBalance(request.OpeningBalance, request.OpeningBalanceDate);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return ToDto(bank);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
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

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
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

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        var span = _tracer.StartSpan($"CashFlow.BankService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
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
