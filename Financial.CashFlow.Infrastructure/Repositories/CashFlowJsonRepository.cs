using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowJsonRepository : ICashFlowRepository, ISyncStatusProvider
{
    private readonly IJsonStorage _storage;
    private readonly ICashFlowSerializer _serializer;
    private readonly CashFlowData _data;

    /// <summary>Serializing the document walks every collection in the graph, so one writer at a
    /// time. A semaphore rather than a lock because the critical section awaits the storage.</summary>
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public CashFlowJsonRepository(CashFlowData data, IJsonStorage storage, ICashFlowSerializer serializer)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public IEnumerable<Expense> GetExpenses() => _data.Expenses;
    public void AddExpense(Expense expense) => _data.AddExpense(expense);
    public void DeleteExpense(Guid id) => _data.RemoveExpense(id);

    public IEnumerable<ReserveMovement> GetReserveMovements() => _data.ReserveMovements;
    public void AddReserveMovement(ReserveMovement movement) => _data.AddReserveMovement(movement);
    public void DeleteReserveMovement(Guid id) => _data.RemoveReserveMovement(id);

    public IEnumerable<CardStatement> GetCardStatements() => _data.CardStatements;
    public void AddCardStatement(CardStatement statement) => _data.AddCardStatement(statement);

    public IEnumerable<RecurringBill> GetRecurringBills() => _data.RecurringBills;
    public void AddRecurringBill(RecurringBill bill) => _data.AddRecurringBill(bill);
    public void DeleteRecurringBill(Guid id) => _data.RemoveRecurringBill(id);

    public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => _data.MaeLedgerEntries;
    public void AddMaeLedgerEntry(MaeLedgerEntry entry) => _data.AddMaeLedgerEntry(entry);
    public void DeleteMaeLedgerEntry(Guid id) => _data.RemoveMaeLedgerEntry(id);

    public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => _data.InvestmentSnapshots;
    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => _data.AddInvestmentSnapshot(snapshot);

    public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => _data.InvestmentAccounts;
    public void AddInvestmentAccount(InvestmentAccount account) => _data.AddInvestmentAccount(account);
    public void DeleteInvestmentAccount(Guid id) => _data.RemoveInvestmentAccount(id);

    public IEnumerable<Bank> GetBanks() => _data.Banks;
    public void AddBank(Bank bank) => _data.AddBank(bank);
    public void DeleteBank(Guid id) => _data.RemoveBank(id);

    public IEnumerable<IncomeSource> GetIncomeSources() => _data.IncomeSources;
    public void AddIncomeSource(IncomeSource incomeSource) => _data.AddIncomeSource(incomeSource);
    public void DeleteIncomeSource(Guid id) => _data.RemoveIncomeSource(id);

    public IEnumerable<ReserveBucket> GetReserveBuckets() => _data.ReserveBuckets;
    public void AddReserveBucket(ReserveBucket bucket) => _data.AddReserveBucket(bucket);

    public IEnumerable<CreditCard> GetCreditCards() => _data.CreditCards;
    public void AddCreditCard(CreditCard card) => _data.AddCreditCard(card);
    public void DeleteCreditCard(Guid id) => _data.RemoveCreditCard(id);

    public IEnumerable<Category> GetCategories() => _data.Categories;
    public void AddCategory(Category category) => _data.AddCategory(category);
    public void DeleteCategory(Guid id) => _data.RemoveCategory(id);

    public IEnumerable<Income> GetIncomes() => _data.Incomes;
    public void AddIncome(Income income) => _data.AddIncome(income);
    public void DeleteIncome(Guid id) => _data.RemoveIncome(id);

    public IEnumerable<Transfer> GetTransfers() => _data.Transfers;
    public void AddTransfer(Transfer transfer) => _data.AddTransfer(transfer);
    public void UpdateTransfer(Transfer transfer) => _data.UpdateTransfer(transfer);
    public void DeleteTransfer(Guid id) => _data.RemoveTransfer(id);

    public IEnumerable<BalanceAdjustment> GetBalanceAdjustments() => _data.BalanceAdjustments;
    public void AddBalanceAdjustment(BalanceAdjustment adjustment) => _data.AddBalanceAdjustment(adjustment);
    public void UpdateBalanceAdjustment(BalanceAdjustment adjustment) => _data.UpdateBalanceAdjustment(adjustment);
    public void DeleteBalanceAdjustment(Guid id) => _data.RemoveBalanceAdjustment(id);

    public async Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges)
    {
        ArgumentNullException.ThrowIfNull(applyChanges);

        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!applyChanges())
            {
                return false;
            }

            // Serializing and writing stay together inside the gate. Splitting them would let a
            // thread holding an older document reach storage after one holding a newer document,
            // discarding the newer change with no error anywhere.
            var json = _serializer.Serialize(_data);
            await _storage.WriteAsync(json).ConfigureAwait(false);
            return true;
        }
        finally
        {
            // Never conditional: skipping this on a storage failure would leave the singleton
            // repository permanently locked, hanging every later save instead of throwing.
            _writeGate.Release();
        }
    }

    public SyncStatus GetStatus() => _storage.GetStatusOrIdle();

    public Task FlushAsync() => _storage.FlushIfSupportedAsync();
}
