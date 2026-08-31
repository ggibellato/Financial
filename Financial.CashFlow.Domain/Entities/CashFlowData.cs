using Financial.CashFlow.Domain.Entities.Collections;
using System;
using System.Collections.Generic;

namespace Financial.CashFlow.Domain.Entities;

public class CashFlowData
{
    private readonly IdCollection<Expense> _expenses = new(i => i.Id);
    public IReadOnlyCollection<Expense> Expenses => _expenses;

    private readonly IdCollection<ReserveMovement> _reserveMovements = new(i => i.Id);
    public IReadOnlyCollection<ReserveMovement> ReserveMovements => _reserveMovements;

    private readonly List<CardStatement> _cardStatements = new();
    public IReadOnlyCollection<CardStatement> CardStatements => _cardStatements.AsReadOnly();

    private readonly IdCollection<RecurringBill> _recurringBills = new(i => i.Id);
    public IReadOnlyCollection<RecurringBill> RecurringBills => _recurringBills;

    private readonly IdCollection<MaeLedgerEntry> _maeLedgerEntries = new(i => i.Id);
    public IReadOnlyCollection<MaeLedgerEntry> MaeLedgerEntries => _maeLedgerEntries;

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

    private readonly IdCollection<Income> _incomes = new(i => i.Id);
    public IReadOnlyCollection<Income> Incomes => _incomes;

    private readonly IdCollection<Transfer> _transfers = new(i => i.Id);
    public IReadOnlyCollection<Transfer> Transfers => _transfers;

    private readonly IdCollection<BalanceAdjustment> _balanceAdjustments = new(i => i.Id);
    public IReadOnlyCollection<BalanceAdjustment> BalanceAdjustments => _balanceAdjustments;

    private readonly List<CreditCard> _creditCards = new();
    public IReadOnlyCollection<CreditCard> CreditCards => _creditCards.AsReadOnly();

    private readonly List<Category> _categories = new();
    public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();

    private CashFlowData() { }

    public static CashFlowData Create() => new();

    public void AddExpense(Expense expense) => _expenses.Add(expense);

    public void RemoveExpense(Guid id) => _expenses.RemoveById(id);

    public void AddReserveMovement(ReserveMovement movement) => _reserveMovements.Add(movement);

    public void RemoveReserveMovement(Guid id) => _reserveMovements.RemoveById(id);

    public void AddCardStatement(CardStatement statement) => _cardStatements.Add(statement);

    public void AddRecurringBill(RecurringBill bill) => _recurringBills.Add(bill);

    public void RemoveRecurringBill(Guid id) => _recurringBills.RemoveById(id);

    public void AddMaeLedgerEntry(MaeLedgerEntry entry) => _maeLedgerEntries.Add(entry);

    public void RemoveMaeLedgerEntry(Guid id) => _maeLedgerEntries.RemoveById(id);

    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => _investmentSnapshots.Add(snapshot);

    public void AddBank(Bank bank) => _banks.Add(bank);

    public void RemoveBank(Guid id) => _banks.RemoveAll(bank => bank.Id == id);

    public void AddIncomeSource(IncomeSource incomeSource) => _incomeSources.Add(incomeSource);

    public void RemoveIncomeSource(Guid id) => _incomeSources.RemoveAll(incomeSource => incomeSource.Id == id);

    public void AddInvestmentAccount(InvestmentAccount account) => _investmentAccounts.Add(account);

    public void AddReserveBucket(ReserveBucket bucket) => _reserveBuckets.Add(bucket);

    public void AddCreditCard(CreditCard card) => _creditCards.Add(card);

    public void RemoveCreditCard(Guid id) => _creditCards.RemoveAll(card => card.Id == id);

    public void AddCategory(Category category) => _categories.Add(category);

    public void RemoveCategory(Guid id) => _categories.RemoveAll(category => category.Id == id);

    public void AddIncome(Income income) => _incomes.Add(income);

    public void RemoveIncome(Guid id) => _incomes.RemoveById(id);

    public void AddTransfer(Transfer transfer) => _transfers.Add(transfer);

    public void UpdateTransfer(Transfer transfer) => _transfers.Update(transfer);

    public void RemoveTransfer(Guid id) => _transfers.RemoveById(id);

    public void AddBalanceAdjustment(BalanceAdjustment adjustment) => _balanceAdjustments.Add(adjustment);

    public void UpdateBalanceAdjustment(BalanceAdjustment adjustment) => _balanceAdjustments.Update(adjustment);

    public void RemoveBalanceAdjustment(Guid id) => _balanceAdjustments.RemoveById(id);
}
