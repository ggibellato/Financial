using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.InvestmentAccounts;

public class InvestmentAccountMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsElevenActiveAndEightDisabledAccounts()
    {
        var data = CashFlowData.Create();

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.AccountsSeededCount.Should().Be(19);
        summary.AccountsAlreadyPresentCount.Should().Be(0);
        data.InvestmentAccounts.Should().HaveCount(19);
        data.InvestmentAccounts.Where(a => a.IsActive).Should().HaveCount(11);
        data.InvestmentAccounts.Where(a => !a.IsActive).Should().HaveCount(8);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "Platinum Visa 8003" && a.IsLiability);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "Chase save" && !a.IsLiability);
        data.InvestmentAccounts.Where(a => !a.IsActive).Should().OnlyContain(a => !a.IsLiability);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "Everyday Saver" && !a.IsActive);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "Instant ISA Issue 1");
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "Chip Cash ISA Gleison");
    }

    [Fact]
    public void Migrate_BlueRewardsSaverAndBarclaysBlueRewards_AreDistinctAccounts()
    {
        var data = CashFlowData.Create();

        InvestmentAccountMigrator.Migrate(data);

        var blueRewardsSaver = data.InvestmentAccounts.Single(a => a.Name == "Blue Rewards Saver");
        var barclaysBlueRewards = data.InvestmentAccounts.Single(a => a.Name == "Barclays Blue Rewards");
        blueRewardsSaver.Id.Should().NotBe(barclaysBlueRewards.Id);
        barclaysBlueRewards.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRun()
    {
        var data = CashFlowData.Create();
        InvestmentAccountMigrator.Migrate(data);

        var secondSummary = InvestmentAccountMigrator.Migrate(data);

        secondSummary.AccountsSeededCount.Should().Be(0);
        secondSummary.AccountsAlreadyPresentCount.Should().Be(19);
        data.InvestmentAccounts.Should().HaveCount(19);
    }

    [Fact]
    public void Migrate_WithSomeAccountsAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddInvestmentAccount(InvestmentAccount.Create("Chase save", isActive: true, isLiability: false));

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.AccountsSeededCount.Should().Be(18);
        summary.AccountsAlreadyPresentCount.Should().Be(1);
        data.InvestmentAccounts.Should().HaveCount(19);
    }

    [Fact]
    public void Migrate_SnapshotWithMatchingAccountReference_CountsAsResolvedAndLeavesValueUntouched()
    {
        // A snapshot's Account is a real reference (F01), so "matching" now means the snapshot was
        // built against an account that's already present in data.InvestmentAccounts (e.g.
        // deserialized from a prior migration run) - not a freshly constructed same-named instance.
        var data = CashFlowData.Create();
        var chaseSave = InvestmentAccount.Create("Chase save", isActive: true, isLiability: false);
        data.AddInvestmentAccount(chaseSave);
        var snapshot = InvestmentSnapshot.Create(chaseSave, 2026, 7, 500m);
        data.AddInvestmentSnapshot(snapshot);

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.SnapshotsResolvedCount.Should().Be(1);
        summary.UnresolvedSnapshots.Should().BeEmpty();
        snapshot.Account.Should().Be(chaseSave);
        snapshot.Value.Should().Be(500m);
    }

    [Fact]
    public void Migrate_SnapshotWithUnresolvableAccountReference_IsFlaggedForManualReviewAndLeftUntouched()
    {
        var data = CashFlowData.Create();
        var unknownAccount = InvestmentAccount.Create("SomeUnknownAccount", isActive: true, isLiability: false);
        var snapshot = InvestmentSnapshot.Create(unknownAccount, 2020, 7, 250m);
        data.AddInvestmentSnapshot(snapshot);

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.UnresolvedSnapshots.Should().ContainSingle().Which.Id.Should().Be(snapshot.Id);
        summary.SnapshotsResolvedCount.Should().Be(0);
        snapshot.Account.Should().Be(unknownAccount);
        snapshot.Value.Should().Be(250m);
    }

    [Fact]
    public void Migrate_SecondRunOverFirstRunsOutput_ChangesNothing()
    {
        var data = CashFlowData.Create();
        var chaseSave = InvestmentAccount.Create("Chase save", isActive: true, isLiability: false);
        data.AddInvestmentAccount(chaseSave);
        var unknownAccount = InvestmentAccount.Create("SomeUnknownAccount", isActive: true, isLiability: false);
        var resolved = InvestmentSnapshot.Create(chaseSave, 2026, 7, 500m);
        var unresolved = InvestmentSnapshot.Create(unknownAccount, 2020, 7, 250m);
        data.AddInvestmentSnapshot(resolved);
        data.AddInvestmentSnapshot(unresolved);

        InvestmentAccountMigrator.Migrate(data);
        var secondSummary = InvestmentAccountMigrator.Migrate(data);

        secondSummary.AccountsSeededCount.Should().Be(0);
        secondSummary.SnapshotsResolvedCount.Should().Be(1);
        secondSummary.UnresolvedSnapshots.Should().ContainSingle().Which.Id.Should().Be(unresolved.Id);
        resolved.Value.Should().Be(500m);
        unresolved.Value.Should().Be(250m);
    }
}
