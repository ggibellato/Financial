using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICashFlowRepository
{
    IEnumerable<Expense> GetExpenses();
    void AddExpense(Expense expense);

    IEnumerable<ReserveMovement> GetReserveMovements();
    void AddReserveMovement(ReserveMovement movement);

    IEnumerable<CardStatement> GetCardStatements();
    void AddCardStatement(CardStatement statement);

    IEnumerable<RecurringBillTemplate> GetRecurringBillTemplates();
    void AddRecurringBillTemplate(RecurringBillTemplate template);

    IEnumerable<RecurringBillInstance> GetRecurringBillInstances();
    void AddRecurringBillInstance(RecurringBillInstance instance);

    IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries();
    void AddMaeLedgerEntry(MaeLedgerEntry entry);

    IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots();
    void AddInvestmentSnapshot(InvestmentSnapshot snapshot);

    Task SaveChangesAsync();
}
