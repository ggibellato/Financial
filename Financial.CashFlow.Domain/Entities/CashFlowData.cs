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

    private List<RecurringBillTemplate> _recurringBillTemplates = new List<RecurringBillTemplate>();
    public IReadOnlyCollection<RecurringBillTemplate> RecurringBillTemplates { get => _recurringBillTemplates.AsReadOnly(); private set => SetRecurringBillTemplates(value); }
    private void SetRecurringBillTemplates(IReadOnlyCollection<RecurringBillTemplate> data)
    {
        _recurringBillTemplates.Clear();
        _recurringBillTemplates.AddRange(data);
    }

    private List<RecurringBillInstance> _recurringBillInstances = new List<RecurringBillInstance>();
    public IReadOnlyCollection<RecurringBillInstance> RecurringBillInstances { get => _recurringBillInstances.AsReadOnly(); private set => SetRecurringBillInstances(value); }
    private void SetRecurringBillInstances(IReadOnlyCollection<RecurringBillInstance> data)
    {
        _recurringBillInstances.Clear();
        _recurringBillInstances.AddRange(data);
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

    private CashFlowData() { }

    public static CashFlowData Create() => new();

    public void AddExpense(Expense expense) => _expenses.Add(expense);

    public void RemoveExpense(Guid id) => _expenses.RemoveAll(e => e.Id == id);

    public void AddReserveMovement(ReserveMovement movement) => _reserveMovements.Add(movement);

    public void RemoveReserveMovement(Guid id) => _reserveMovements.RemoveAll(m => m.Id == id);

    public void AddCardStatement(CardStatement statement) => _cardStatements.Add(statement);

    public void AddRecurringBillTemplate(RecurringBillTemplate template) => _recurringBillTemplates.Add(template);

    public void RemoveRecurringBillTemplate(Guid id) => _recurringBillTemplates.RemoveAll(t => t.Id == id);

    public void AddRecurringBillInstance(RecurringBillInstance instance) => _recurringBillInstances.Add(instance);

    public void RemoveRecurringBillInstance(Guid id) => _recurringBillInstances.RemoveAll(i => i.Id == id);

    public void AddMaeLedgerEntry(MaeLedgerEntry entry) => _maeLedgerEntries.Add(entry);

    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => _investmentSnapshots.Add(snapshot);
}
