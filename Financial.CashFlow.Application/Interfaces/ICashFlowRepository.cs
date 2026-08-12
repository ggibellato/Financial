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

    IEnumerable<InvestmentAccount> GetInvestmentAccounts();
    void AddInvestmentAccount(InvestmentAccount account);

    IEnumerable<Bank> GetBanks();

    IEnumerable<IncomeSource> GetIncomeSources();

    IEnumerable<ReserveBucket> GetReserveBuckets();

    IEnumerable<CreditCard> GetCreditCards();

    IEnumerable<Category> GetCategories();

    IEnumerable<Income> GetIncomes();
    void AddIncome(Income income);
    void DeleteIncome(Guid id);

    IEnumerable<Transfer> GetTransfers();
    void AddTransfer(Transfer transfer);
    void UpdateTransfer(Transfer transfer);
    void DeleteTransfer(Guid id);

    IEnumerable<BalanceAdjustment> GetBalanceAdjustments();
    void AddBalanceAdjustment(BalanceAdjustment adjustment);
    void UpdateBalanceAdjustment(BalanceAdjustment adjustment);
    void DeleteBalanceAdjustment(Guid id);

    Task SaveChangesAsync();
}
