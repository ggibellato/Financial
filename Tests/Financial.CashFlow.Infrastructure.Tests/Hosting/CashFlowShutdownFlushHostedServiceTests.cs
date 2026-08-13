using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Hosting;
using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Hosting;

public class CashFlowShutdownFlushHostedServiceTests
{
    [Fact]
    public async Task StopAsync_WhenRepositoryIsASyncStatusProvider_CallsFlushAsync()
    {
        var repository = new SyncStatusStubCashFlowRepository();
        var hostedService = new CashFlowShutdownFlushHostedService(repository);

        await hostedService.StopAsync(CancellationToken.None);

        repository.FlushAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_WhenRepositoryIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var repository = new PlainStubCashFlowRepository();
        var hostedService = new CashFlowShutdownFlushHostedService(repository);

        var act = async () => await hostedService.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CashFlowShutdownFlushHostedService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    private sealed class SyncStatusStubCashFlowRepository : ICashFlowRepository, ISyncStatusProvider
    {
        internal int FlushAsyncCallCount { get; private set; }

        public SyncStatus GetStatus() => new(SyncState.Idle, null, null);

        public Task FlushAsync()
        {
            FlushAsyncCallCount++;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => throw new NotImplementedException();
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
        public IEnumerable<IncomeSource> GetIncomeSources() => throw new NotImplementedException();
        public IEnumerable<ReserveBucket> GetReserveBuckets() => throw new NotImplementedException();
        public IEnumerable<CreditCard> GetCreditCards() => throw new NotImplementedException();
        public IEnumerable<Category> GetCategories() => throw new NotImplementedException();
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

    private sealed class PlainStubCashFlowRepository : ICashFlowRepository
    {
        public Task SaveChangesAsync() => throw new NotImplementedException();
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
        public IEnumerable<IncomeSource> GetIncomeSources() => throw new NotImplementedException();
        public IEnumerable<ReserveBucket> GetReserveBuckets() => throw new NotImplementedException();
        public IEnumerable<CreditCard> GetCreditCards() => throw new NotImplementedException();
        public IEnumerable<Category> GetCategories() => throw new NotImplementedException();
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
}
