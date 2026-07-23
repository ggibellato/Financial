using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowJsonRepository : ICashFlowRepository
{
    private readonly IJsonStorage _storage;
    private readonly ICashFlowSerializer _serializer;
    private readonly CashFlowData _data;

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

    public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => _data.InvestmentSnapshots;
    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => _data.AddInvestmentSnapshot(snapshot);

    public async Task SaveChangesAsync()
    {
        var json = _serializer.Serialize(_data);
        await _storage.WriteAsync(json).ConfigureAwait(false);
    }
}
