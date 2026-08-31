using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Sync;

namespace Financial.TestUtilities;

/// <summary>
/// An <see cref="ICashFlowRepository"/> whose only implemented behavior is reporting/flushing sync
/// status - every CRUD member throws <see cref="NotImplementedException"/>. For tests that need a
/// repository double implementing <see cref="ISyncStatusProvider"/> specifically (shutdown-flush
/// wiring, the sync-status endpoint); for the "repository is NOT an ISyncStatusProvider" case, use
/// <see cref="StubCashFlowRepository"/> instead, which deliberately doesn't implement it.
/// </summary>
public sealed class SyncStatusCashFlowRepositoryStub : ICashFlowRepository, ISyncStatusProvider
{
    public SyncStatus StatusToReturn { get; set; } = new(SyncState.Idle, null, null);

    public int FlushAsyncCallCount { get; private set; }

    public SyncStatus GetStatus() => StatusToReturn;

    public Task FlushAsync()
    {
        FlushAsyncCallCount++;
        return Task.CompletedTask;
    }

    public Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges) => throw new NotImplementedException();
    public IEnumerable<Expense> GetExpenses() => throw new NotImplementedException();
    public void AddExpense(Expense expense) => throw new NotImplementedException();
    public void DeleteExpense(Guid id) => throw new NotImplementedException();
    public IEnumerable<ReserveMovement> GetReserveMovements() => throw new NotImplementedException();
    public void AddReserveMovement(ReserveMovement movement) => throw new NotImplementedException();
    public void DeleteReserveMovement(Guid id) => throw new NotImplementedException();
    public IEnumerable<CardStatement> GetCardStatements() => throw new NotImplementedException();
    public void AddCardStatement(CardStatement statement) => throw new NotImplementedException();
    public IEnumerable<RecurringBill> GetRecurringBills() => throw new NotImplementedException();
    public void AddRecurringBill(RecurringBill bill) => throw new NotImplementedException();
    public void DeleteRecurringBill(Guid id) => throw new NotImplementedException();
    public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => throw new NotImplementedException();
    public void AddMaeLedgerEntry(MaeLedgerEntry entry) => throw new NotImplementedException();
    public void DeleteMaeLedgerEntry(Guid id) => throw new NotImplementedException();
    public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => throw new NotImplementedException();
    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => throw new NotImplementedException();
    public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => throw new NotImplementedException();
    public void AddInvestmentAccount(InvestmentAccount account) => throw new NotImplementedException();
    public IEnumerable<Bank> GetBanks() => throw new NotImplementedException();
    public void AddBank(Bank bank) => throw new NotImplementedException();
    public void DeleteBank(Guid id) => throw new NotImplementedException();
    public IEnumerable<IncomeSource> GetIncomeSources() => throw new NotImplementedException();
    public IEnumerable<ReserveBucket> GetReserveBuckets() => throw new NotImplementedException();
    public IEnumerable<CreditCard> GetCreditCards() => throw new NotImplementedException();
    public void AddCreditCard(CreditCard card) => throw new NotImplementedException();
    public void DeleteCreditCard(Guid id) => throw new NotImplementedException();
    public IEnumerable<Category> GetCategories() => throw new NotImplementedException();
    public void AddCategory(Category category) => throw new NotImplementedException();
    public void DeleteCategory(Guid id) => throw new NotImplementedException();
    public IEnumerable<Income> GetIncomes() => throw new NotImplementedException();
    public void AddIncome(Income income) => throw new NotImplementedException();
    public void DeleteIncome(Guid id) => throw new NotImplementedException();
    public IEnumerable<Transfer> GetTransfers() => throw new NotImplementedException();
    public void AddTransfer(Transfer transfer) => throw new NotImplementedException();
    public void UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
    public void DeleteTransfer(Guid id) => throw new NotImplementedException();
    public IEnumerable<BalanceAdjustment> GetBalanceAdjustments() => throw new NotImplementedException();
    public void AddBalanceAdjustment(BalanceAdjustment adjustment) => throw new NotImplementedException();
    public void UpdateBalanceAdjustment(BalanceAdjustment adjustment) => throw new NotImplementedException();
    public void DeleteBalanceAdjustment(Guid id) => throw new NotImplementedException();
}
