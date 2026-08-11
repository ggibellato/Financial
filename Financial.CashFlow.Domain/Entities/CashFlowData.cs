using System;
using System.Collections.Generic;

namespace Financial.CashFlow.Domain.Entities;

public class CashFlowData
{
    private readonly List<Expense> _expenses = new();
    public IReadOnlyCollection<Expense> Expenses => _expenses.AsReadOnly();

    private readonly List<ReserveMovement> _reserveMovements = new();
    public IReadOnlyCollection<ReserveMovement> ReserveMovements => _reserveMovements.AsReadOnly();

    private readonly List<CardStatement> _cardStatements = new();
    public IReadOnlyCollection<CardStatement> CardStatements => _cardStatements.AsReadOnly();

    private readonly List<RecurringBill> _recurringBills = new();
    public IReadOnlyCollection<RecurringBill> RecurringBills => _recurringBills.AsReadOnly();

    private readonly List<MaeLedgerEntry> _maeLedgerEntries = new();
    public IReadOnlyCollection<MaeLedgerEntry> MaeLedgerEntries => _maeLedgerEntries.AsReadOnly();

    private readonly List<InvestmentSnapshot> _investmentSnapshots = new();
    public IReadOnlyCollection<InvestmentSnapshot> InvestmentSnapshots => _investmentSnapshots.AsReadOnly();

    private readonly List<Bank> _banks = new();
    public IReadOnlyCollection<Bank> Banks => _banks.AsReadOnly();

    private readonly List<IncomeSource> _incomeSources = new();
    public IReadOnlyCollection<IncomeSource> IncomeSources => _incomeSources.AsReadOnly();

    private readonly List<InvestmentAccount> _investmentAccounts = new();
    public IReadOnlyCollection<InvestmentAccount> InvestmentAccounts => _investmentAccounts.AsReadOnly();

    private readonly List<ReserveBucket> _reserveBuckets = new();
    public IReadOnlyCollection<ReserveBucket> ReserveBuckets => _reserveBuckets.AsReadOnly();

    private readonly List<Income> _incomes = new();
    public IReadOnlyCollection<Income> Incomes => _incomes.AsReadOnly();

    private readonly List<Transfer> _transfers = new();
    public IReadOnlyCollection<Transfer> Transfers => _transfers.AsReadOnly();

    private readonly List<BalanceAdjustment> _balanceAdjustments = new();
    public IReadOnlyCollection<BalanceAdjustment> BalanceAdjustments => _balanceAdjustments.AsReadOnly();

    private readonly List<CreditCard> _creditCards = new();
    public IReadOnlyCollection<CreditCard> CreditCards => _creditCards.AsReadOnly();

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

    public void AddReserveBucket(ReserveBucket bucket) => _reserveBuckets.Add(bucket);

    public void AddCreditCard(CreditCard card) => _creditCards.Add(card);

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
