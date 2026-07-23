using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class InvestmentSnapshotServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentSnapshotService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_FirstCall_GeneratesExactlyElevenSnapshotsDefaultingToZero()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(2026, 7);

        result.Should().HaveCount(11);
        result.Should().OnlyContain(s => s.Value == 0m);
        repository.Snapshots.Should().HaveCount(11);
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_MarksTheSixLiabilityAccountsCorrectly()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(2026, 7);

        result.Where(s => s.IsLiability).Should().HaveCount(6);
        result.Should().ContainSingle(s => s.Account == "PlatinumVisa8003" && s.IsLiability);
        result.Should().ContainSingle(s => s.Account == "ReservasPessoais" && s.IsLiability);
        result.Should().ContainSingle(s => s.Account == "ChaseSave" && !s.IsLiability);
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);

        await service.GetSnapshotsForMonthAsync(2026, 7);
        var result = await service.GetSnapshotsForMonthAsync(2026, 7);

        result.Should().HaveCount(11);
        repository.Snapshots.Should().HaveCount(11);
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_UpdatesOnlyTheTargetedSnapshot()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);
        await service.GetSnapshotsForMonthAsync(2026, 7);
        await service.GetSnapshotsForMonthAsync(2026, 8);
        var julySnapshot = repository.Snapshots.Single(s => s.Month == 7 && s.Account == InvestmentAccount.ChaseSave);
        var augustSnapshot = repository.Snapshots.Single(s => s.Month == 8 && s.Account == InvestmentAccount.ChaseSave);
        var otherAccountSnapshot = repository.Snapshots.Single(s => s.Month == 7 && s.Account == InvestmentAccount.PlatinumVisa8003);

        var result = await service.UpdateSnapshotValueAsync(julySnapshot.Id, new UpdateInvestmentSnapshotValueDTO { Value = 500m });

        result.Value.Should().Be(500m);
        augustSnapshot.Value.Should().Be(0m);
        otherAccountSnapshot.Value.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithNegativeValue_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);
        await service.GetSnapshotsForMonthAsync(2026, 7);
        var snapshot = repository.Snapshots.First();

        var act = async () => await service.UpdateSnapshotValueAsync(snapshot.Id, new UpdateInvestmentSnapshotValueDTO { Value = -1m });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new InvestmentSnapshotService(new StubCashFlowRepository());

        var act = async () => await service.UpdateSnapshotValueAsync(Guid.NewGuid(), new UpdateInvestmentSnapshotValueDTO { Value = 10m });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<InvestmentSnapshot> Snapshots { get; } = new();
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBillTemplate> GetRecurringBillTemplates() => Array.Empty<RecurringBillTemplate>();
        public void AddRecurringBillTemplate(RecurringBillTemplate template) { }

        public IEnumerable<RecurringBillInstance> GetRecurringBillInstances() => Array.Empty<RecurringBillInstance>();
        public void AddRecurringBillInstance(RecurringBillInstance instance) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Snapshots;
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => Snapshots.Add(snapshot);

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
