using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class BalanceAdjustmentService : IBalanceAdjustmentService
{
    private readonly ICashFlowRepository _repository;

    public BalanceAdjustmentService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<BalanceAdjustmentDTO> AddAdjustmentAsync(string bankName, BalanceAdjustmentCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bank = ResolveBank(bankName);
        var currentBalance = ComputeBalanceAsOf(bank.Name, request.Date, excludingAdjustmentId: null);
        var delta = request.TargetBalance - currentBalance;

        var adjustment = BalanceAdjustment.Create(request.Date, bank.Name, request.TargetBalance, delta, request.Note);
        _repository.AddBalanceAdjustment(adjustment);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(adjustment);
    }

    public async Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(string bankName, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bank = ResolveBank(bankName);
        var adjustment = FindAdjustmentOrThrow(bank.Name, id);
        var currentBalance = ComputeBalanceAsOf(bank.Name, request.Date, excludingAdjustmentId: id);
        var delta = request.TargetBalance - currentBalance;

        adjustment.UpdateDetails(request.Date, request.TargetBalance, delta, request.Note);
        _repository.UpdateBalanceAdjustment(adjustment);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(adjustment);
    }

    public async Task DeleteAdjustmentAsync(string bankName, Guid id)
    {
        var bank = ResolveBank(bankName);
        FindAdjustmentOrThrow(bank.Name, id);

        _repository.DeleteBalanceAdjustment(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(string bankName)
    {
        if (!BankNameResolver.TryResolve(bankName, _repository.GetBanks(), out var bank))
        {
            return Array.Empty<BalanceAdjustmentDTO>();
        }

        return _repository.GetBalanceAdjustments()
            .Where(a => string.Equals(a.Bank, bank!.Name, StringComparison.OrdinalIgnoreCase))
            .Select(ToDto)
            .ToList();
    }

    private Bank ResolveBank(string bankName)
    {
        if (!BankNameResolver.TryResolve(bankName, _repository.GetBanks(), out var bank))
        {
            throw new ArgumentException($"Bank '{bankName}' was not found.");
        }

        return bank!;
    }

    private BalanceAdjustment FindAdjustmentOrThrow(string bankName, Guid id) =>
        _repository.GetBalanceAdjustments()
            .FirstOrDefault(a => a.Id == id && string.Equals(a.Bank, bankName, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Balance adjustment '{id}' was not found.");

    /// <summary>
    /// Interim balance-as-of-date calculation, mirroring BankService.GetBankBalancesByMonth's formula
    /// plus the running total of other adjustments already recorded for this bank.
    /// F03 replaces this with the shared, transfer-aware IBankService.GetBankBalanceAsOf.
    /// </summary>
    private decimal ComputeBalanceAsOf(string bankName, DateOnly asOfDate, Guid? excludingAdjustmentId)
    {
        var bank = _repository.GetBanks().First(b => string.Equals(b.Name, bankName, StringComparison.OrdinalIgnoreCase));

        var incomeTotal = _repository.GetIncomes()
            .Where(i => i.Bank == bank.Name && i.Date >= bank.OpeningBalanceDate && i.Date <= asOfDate)
            .Sum(i => i.NetValue);

        var expenseTotal = _repository.GetExpenses()
            .Where(e => e.PaymentSource == bank.Name && e.Date >= bank.OpeningBalanceDate && e.Date <= asOfDate)
            .Sum(e => e.Value - (e.RoundUpAmount ?? 0));

        var adjustmentTotal = _repository.GetBalanceAdjustments()
            .Where(a =>
                string.Equals(a.Bank, bank.Name, StringComparison.OrdinalIgnoreCase) &&
                a.Date <= asOfDate &&
                a.Id != excludingAdjustmentId)
            .Sum(a => a.Delta);

        return bank.OpeningBalance + incomeTotal - expenseTotal + adjustmentTotal;
    }

    private static BalanceAdjustmentDTO ToDto(BalanceAdjustment adjustment) => new()
    {
        Id = adjustment.Id,
        Date = adjustment.Date,
        Bank = adjustment.Bank,
        TargetBalance = adjustment.TargetBalance,
        Delta = adjustment.Delta,
        Note = adjustment.Note
    };
}
