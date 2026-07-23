using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICashFlowRepository
{
    IEnumerable<Expense> GetExpenses();
    void AddExpense(Expense expense);
    void DeleteExpense(Guid id);

    IEnumerable<ReserveMovement> GetReserveMovements();
    void AddReserveMovement(ReserveMovement movement);
    void DeleteReserveMovement(Guid id);

    IEnumerable<CardStatement> GetCardStatements();
    void AddCardStatement(CardStatement statement);

    IEnumerable<RecurringBill> GetRecurringBills();
    void AddRecurringBill(RecurringBill bill);
    void DeleteRecurringBill(Guid id);

    IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries();
    void AddMaeLedgerEntry(MaeLedgerEntry entry);
    void DeleteMaeLedgerEntry(Guid id);

    IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots();
    void AddInvestmentSnapshot(InvestmentSnapshot snapshot);

    Task SaveChangesAsync();
}
