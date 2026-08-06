using System;
using System.Collections.Generic;

namespace Financial.CashFlow.Domain.Entities;

public class CashFlowData
{
    private List<Expense> _expenses = new List<Expense>();
    public IReadOnlyCollection<Expense> Expenses { get => _expenses.AsReadOnly(); private set => SetExpenses(value); }
    private void SetExpenses(IReadOnlyCollection<Expense> data)
    {
        _expenses.Clear();
        _expenses.AddRange(data);
    }

    private List<ReserveMovement> _reserveMovements = new List<ReserveMovement>();
    public IReadOnlyCollection<ReserveMovement> ReserveMovements { get => _reserveMovements.AsReadOnly(); private set => SetReserveMovements(value); }
    private void SetReserveMovements(IReadOnlyCollection<ReserveMovement> data)
    {
        _reserveMovements.Clear();
        _reserveMovements.AddRange(data);
    }

    private List<CardStatement> _cardStatements = new List<CardStatement>();
    public IReadOnlyCollection<CardStatement> CardStatements { get => _cardStatements.AsReadOnly(); private set => SetCardStatements(value); }
    private void SetCardStatements(IReadOnlyCollection<CardStatement> data)
    {
        _cardStatements.Clear();
        _cardStatements.AddRange(data);
    }

    private List<RecurringBill> _recurringBills = new List<RecurringBill>();
    public IReadOnlyCollection<RecurringBill> RecurringBills { get => _recurringBills.AsReadOnly(); private set => SetRecurringBills(value); }
    private void SetRecurringBills(IReadOnlyCollection<RecurringBill> data)
    {
        _recurringBills.Clear();
        _recurringBills.AddRange(data);
    }

    private List<MaeLedgerEntry> _maeLedgerEntries = new List<MaeLedgerEntry>();
    public IReadOnlyCollection<MaeLedgerEntry> MaeLedgerEntries { get => _maeLedgerEntries.AsReadOnly(); private set => SetMaeLedgerEntries(value); }
    private void SetMaeLedgerEntries(IReadOnlyCollection<MaeLedgerEntry> data)
    {
        _maeLedgerEntries.Clear();
        _maeLedgerEntries.AddRange(data);
    }

    private List<InvestmentSnapshot> _investmentSnapshots = new List<InvestmentSnapshot>();
    public IReadOnlyCollection<InvestmentSnapshot> InvestmentSnapshots { get => _investmentSnapshots.AsReadOnly(); private set => SetInvestmentSnapshots(value); }
    private void SetInvestmentSnapshots(IReadOnlyCollection<InvestmentSnapshot> data)
    {
        _investmentSnapshots.Clear();
        _investmentSnapshots.AddRange(data);
    }

    private List<Bank> _banks = new List<Bank>();
    public IReadOnlyCollection<Bank> Banks { get => _banks.AsReadOnly(); private set => SetBanks(value); }
    private void SetBanks(IReadOnlyCollection<Bank> data)
    {
        _banks.Clear();
        _banks.AddRange(data);
    }

    private List<IncomeSource> _incomeSources = new List<IncomeSource>();
    public IReadOnlyCollection<IncomeSource> IncomeSources { get => _incomeSources.AsReadOnly(); private set => SetIncomeSources(value); }
    private void SetIncomeSources(IReadOnlyCollection<IncomeSource> data)
    {
        _incomeSources.Clear();
        _incomeSources.AddRange(data);
    }

    private List<InvestmentAccount> _investmentAccounts = new List<InvestmentAccount>();
    public IReadOnlyCollection<InvestmentAccount> InvestmentAccounts { get => _investmentAccounts.AsReadOnly(); private set => SetInvestmentAccounts(value); }
    private void SetInvestmentAccounts(IReadOnlyCollection<InvestmentAccount> data)
    {
        _investmentAccounts.Clear();
        _investmentAccounts.AddRange(data);
    }

    private List<Income> _incomes = new List<Income>();
    public IReadOnlyCollection<Income> Incomes { get => _incomes.AsReadOnly(); private set => SetIncomes(value); }
    private void SetIncomes(IReadOnlyCollection<Income> data)
    {
        _incomes.Clear();
        _incomes.AddRange(data);
    }

    private List<Transfer> _transfers = new List<Transfer>();
    public IReadOnlyCollection<Transfer> Transfers { get => _transfers.AsReadOnly(); private set => SetTransfers(value); }
    private void SetTransfers(IReadOnlyCollection<Transfer> data)
    {
        _transfers.Clear();
        _transfers.AddRange(data);
    }

    private List<BalanceAdjustment> _balanceAdjustments = new List<BalanceAdjustment>();
    public IReadOnlyCollection<BalanceAdjustment> BalanceAdjustments { get => _balanceAdjustments.AsReadOnly(); private set => SetBalanceAdjustments(value); }
    private void SetBalanceAdjustments(IReadOnlyCollection<BalanceAdjustment> data)
    {
        _balanceAdjustments.Clear();
        _balanceAdjustments.AddRange(data);
    }

    private CashFlowData() { }

    public static CashFlowData Create() => new();

    public void AddExpense(Expense expense) => _expenses.Add(expense);

    public void RemoveExpense(Guid id) => _expenses.RemoveAll(e => e.Id == id);

    public void AddReserveMovement(ReserveMovement movement) => _reserveMovements.Add(movement);

    public void RemoveReserveMovement(Guid id) => _reserveMovements.RemoveAll(m => m.Id == id);

    public void AddCardStatement(CardStatement statement) => _cardStatements.Add(statement);

    public void AddRecurringBill(RecurringBill bill) => _recurringBills.Add(bill);

    public void RemoveRecurringBill(Guid id) => _recurringBills.RemoveAll(b => b.Id == id);

    public void AddMaeLedgerEntry(MaeLedgerEntry entry) => _maeLedgerEntries.Add(entry);

    public void RemoveMaeLedgerEntry(Guid id) => _maeLedgerEntries.RemoveAll(e => e.Id == id);

    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => _investmentSnapshots.Add(snapshot);

    public void AddBank(Bank bank) => _banks.Add(bank);

    public void AddIncomeSource(IncomeSource incomeSource) => _incomeSources.Add(incomeSource);

    public void AddInvestmentAccount(InvestmentAccount account) => _investmentAccounts.Add(account);

    public void AddIncome(Income income) => _incomes.Add(income);

    public void RemoveIncome(Guid id) => _incomes.RemoveAll(i => i.Id == id);

    public void AddTransfer(Transfer transfer) => _transfers.Add(transfer);

    public void UpdateTransfer(Transfer transfer)
    {
        var index = _transfers.FindIndex(t => t.Id == transfer.Id);
        if (index >= 0)
        {
            _transfers[index] = transfer;
        }
    }

    public void RemoveTransfer(Guid id) => _transfers.RemoveAll(t => t.Id == id);

    public void AddBalanceAdjustment(BalanceAdjustment adjustment) => _balanceAdjustments.Add(adjustment);

    public void UpdateBalanceAdjustment(BalanceAdjustment adjustment)
    {
        var index = _balanceAdjustments.FindIndex(a => a.Id == adjustment.Id);
        if (index >= 0)
        {
            _balanceAdjustments[index] = adjustment;
        }
    }

    public void RemoveBalanceAdjustment(Guid id) => _balanceAdjustments.RemoveAll(a => a.Id == id);
}
