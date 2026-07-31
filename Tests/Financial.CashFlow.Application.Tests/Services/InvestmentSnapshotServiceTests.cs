using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class InvestmentSnapshotServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;

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

        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(11);
            result.Should().OnlyContain(s => s.Value == 0m);
            repository.Snapshots.Should().HaveCount(11);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_MarksTheSixLiabilityAccountsCorrectly()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Where(s => s.IsLiability).Should().HaveCount(6);
            result.Should().ContainSingle(s => s.Account == "PlatinumVisa8003" && s.IsLiability);
            result.Should().ContainSingle(s => s.Account == "ReservasPessoais" && s.IsLiability);
            result.Should().ContainSingle(s => s.Account == "ChaseSave" && !s.IsLiability);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);

        await service.GetSnapshotsForMonthAsync(CurrentYear, 7);
        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(11);
            repository.Snapshots.Should().HaveCount(11);
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_CurrentYear_ExcludesDisabledAccounts()
    {
        var repository = new StubCashFlowRepository();
        repository.Accounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        result.Should().HaveCount(11);
        result.Should().NotContain(s => s.Account == "EverydaySaver");
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_PastYearWithNoExistingData_ReturnsEmptyNotAllAccounts()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(PastYear, 7);

        result.Should().BeEmpty();
        repository.Snapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_PastYearWithSomeAccountsPresent_ReturnsOnlyThose()
    {
        var repository = new StubCashFlowRepository();
        repository.Snapshots.Add(InvestmentSnapshot.Create("ChaseSave", PastYear, 7, 100m));
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(PastYear, 7);

        result.Should().ContainSingle().Which.Account.Should().Be("ChaseSave");
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_UpdatesOnlyTheTargetedSnapshot()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);
        await service.GetSnapshotsForMonthAsync(CurrentYear, 7);
        await service.GetSnapshotsForMonthAsync(CurrentYear, 8);
        var julySnapshot = repository.Snapshots.Single(s => s.Month == 7 && s.Account == "ChaseSave");
        var augustSnapshot = repository.Snapshots.Single(s => s.Month == 8 && s.Account == "ChaseSave");
        var otherAccountSnapshot = repository.Snapshots.Single(s => s.Month == 7 && s.Account == "PlatinumVisa8003");

        var result = await service.UpdateSnapshotValueAsync(julySnapshot.Id, new UpdateInvestmentSnapshotValueDTO { Value = 500m });

        using (new AssertionScope())
        {
            result.Value.Should().Be(500m);
            augustSnapshot.Value.Should().Be(0m);
            otherAccountSnapshot.Value.Should().Be(0m);
        }
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithNegativeValue_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        var service = new InvestmentSnapshotService(repository);
        await service.GetSnapshotsForMonthAsync(CurrentYear, 7);
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
        private static readonly (string Name, bool IsLiability)[] SeededAccounts =
        [
            ("BlueRewardsSaver", false),
            ("PlatinumVisa8003", true),
            ("PlatinumVisa6007", true),
            ("ChaseMaster4023", true),
            ("BaAmex", true),
            ("PaypalCredit", true),
            ("ChipCashIsaGleison", false),
            ("ChaseSave", false),
            ("ChipCashIsaAriana", false),
            ("Trading212Invested", false),
            ("ReservasPessoais", true)
        ];

        public List<InvestmentSnapshot> Snapshots { get; } = new();
        public List<InvestmentAccount> Accounts { get; } =
            SeededAccounts.Select(a => InvestmentAccount.Create(a.Name, isActive: true, isLiability: a.IsLiability)).ToList();
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBill> GetRecurringBills() => Array.Empty<RecurringBill>();
        public void AddRecurringBill(RecurringBill bill) { }
        public void DeleteRecurringBill(Guid id) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }
        public void DeleteMaeLedgerEntry(Guid id) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Snapshots;
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => Snapshots.Add(snapshot);

        public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => Accounts;
        public void AddInvestmentAccount(InvestmentAccount account) => Accounts.Add(account);

        public IEnumerable<Bank> GetBanks() => Array.Empty<Bank>();

        public IEnumerable<Income> GetIncomes() => Array.Empty<Income>();
        public void AddIncome(Income income) { }
        public void DeleteIncome(Guid id) { }

        public IEnumerable<Transfer> GetTransfers() => Array.Empty<Transfer>();
        public void AddTransfer(Transfer transfer) { }
        public void UpdateTransfer(Transfer transfer) { }
        public void DeleteTransfer(Guid id) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
