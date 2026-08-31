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
    void AddBank(Bank bank);
    void DeleteBank(Guid id);

    IEnumerable<IncomeSource> GetIncomeSources();
    void AddIncomeSource(IncomeSource incomeSource);
    void DeleteIncomeSource(Guid id);

    IEnumerable<ReserveBucket> GetReserveBuckets();

    IEnumerable<CreditCard> GetCreditCards();
    void AddCreditCard(CreditCard card);
    void DeleteCreditCard(Guid id);

    IEnumerable<Category> GetCategories();
    void AddCategory(Category category);
    void DeleteCategory(Guid id);

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

    /// <summary>
    /// Runs <paramref name="applyChanges"/> with exclusive access to the in-memory data, then
    /// persists the whole document when it reports a change.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Saving re-serializes the entire object graph, so a mutation running on another thread part
    /// way through that walk breaks it. Handing the mutation in is what puts both under the same
    /// exclusion; mutating an entity first and saving afterwards does not.
    /// </para>
    /// <para>
    /// The delegate must be synchronous, and must not call back into this method: the exclusion is
    /// not reentrant, so a nested call deadlocks the process rather than throwing. Calling the
    /// plain mutators above from inside it is expected and safe - they are not gated themselves.
    /// </para>
    /// </remarks>
    /// <param name="applyChanges">
    /// Applies the change and returns true when something changed and must be persisted. Returning
    /// false leaves the document untouched, which is how an in-memory-only correction such as a
    /// compensating rollback runs under the same exclusion without writing.
    /// </param>
    /// <returns>What <paramref name="applyChanges"/> reported: true when the document was written.</returns>
    Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges);
}
