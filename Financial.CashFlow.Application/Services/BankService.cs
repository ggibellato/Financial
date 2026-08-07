using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class BankService : IBankService
{
    private readonly ICashFlowRepository _repository;

    public BankService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<BankDTO> GetBanks() =>
        _repository.GetBanks().Select(ToDto).ToList();

    public async Task<BankDTO> UpdateOpeningBalanceAsync(Guid id, BankOpeningBalanceUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!BankNameResolver.TryResolve(id, _repository.GetBanks(), out var bank))
        {
            throw new KeyNotFoundException($"Bank '{id}' was not found.");
        }

        bank!.SetOpeningBalance(request.OpeningBalance, request.OpeningBalanceDate);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(bank);
    }

    public IReadOnlyList<BankBalanceDTO> GetBankBalancesByMonth(int year, int month)
    {
        var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var incomes = _repository.GetIncomes().ToList();
        var expenses = _repository.GetExpenses().ToList();
        var transfers = _repository.GetTransfers().ToList();
        var adjustments = _repository.GetBalanceAdjustments().ToList();

        return _repository.GetBanks()
            .Select(bank => new BankBalanceDTO
            {
                Bank = bank.Name,
                Balance = ComputeBalance(bank, endOfMonth, incomes, expenses, transfers, adjustments, excludingAdjustmentId: null)
            })
            .ToList();
    }

    public decimal GetBankBalanceAsOf(Guid bankId, DateOnly asOfDate, Guid? excludingAdjustmentId = null)
    {
        if (!BankNameResolver.TryResolve(bankId, _repository.GetBanks(), out var bank))
        {
            throw new KeyNotFoundException($"Bank '{bankId}' was not found.");
        }

        return ComputeBalance(
            bank!,
            asOfDate,
            _repository.GetIncomes(),
            _repository.GetExpenses(),
            _repository.GetTransfers(),
            _repository.GetBalanceAdjustments(),
            excludingAdjustmentId);
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
            .Where(i => i.Bank.Id == bank.Id && InWindow(i.Date))
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
