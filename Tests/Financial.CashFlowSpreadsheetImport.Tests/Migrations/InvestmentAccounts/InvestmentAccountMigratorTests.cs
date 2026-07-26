using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.InvestmentAccounts;

public class InvestmentAccountMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsElevenActiveAndEightDisabledAccountsWithAliases()
    {
        var data = CashFlowData.Create();

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.AccountsSeededCount.Should().Be(19);
        summary.AccountsAlreadyPresentCount.Should().Be(0);
        data.InvestmentAccounts.Should().HaveCount(19);
        data.InvestmentAccounts.Where(a => a.IsActive).Should().HaveCount(11);
        data.InvestmentAccounts.Where(a => !a.IsActive).Should().HaveCount(8);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "PlatinumVisa8003" && a.IsLiability);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChaseSave" && !a.IsLiability);
        data.InvestmentAccounts.Where(a => !a.IsActive).Should().OnlyContain(a => !a.IsLiability);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "EverydaySaver" && !a.IsActive)
            .Which.Aliases.Should().ContainSingle("Everyday Saver");
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "InstantIsaIssue1")
            .Which.Aliases.Should().BeEquivalentTo("Instant ISA Issue 1", "Instant ISE Issue 1");
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChipCashIsaGleison")
            .Which.Aliases.Should().BeEquivalentTo("Chip Cash ISA Gleison", "Chip Cash ISA");
    }

    [Fact]
    public void Migrate_BlueRewardsSaverAndBarclaysBlueRewards_HaveDistinctNonOverlappingAliases()
    {
        var data = CashFlowData.Create();

        InvestmentAccountMigrator.Migrate(data);

        var blueRewardsSaver = data.InvestmentAccounts.Single(a => a.Name == "BlueRewardsSaver");
        var barclaysBlueRewards = data.InvestmentAccounts.Single(a => a.Name == "BarclaysBlueRewards");
        blueRewardsSaver.Aliases.Should().BeEquivalentTo("Blue Rewards Saver");
        barclaysBlueRewards.Aliases.Should().BeEquivalentTo("Barclays Blue Rewards");
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
        data.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.AccountsSeededCount.Should().Be(18);
        summary.AccountsAlreadyPresentCount.Should().Be(1);
        data.InvestmentAccounts.Should().HaveCount(19);
    }

    [Fact]
    public void Migrate_AccountSeededByPriorRunWithNoAliases_BackfillsAliasesWithoutDuplicating()
    {
        var data = CashFlowData.Create();
        var preExisting = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        data.AddInvestmentAccount(preExisting);
        preExisting.Aliases.Should().BeEmpty();

        InvestmentAccountMigrator.Migrate(data);

        preExisting.Aliases.Should().BeEquivalentTo("Chase save");

        InvestmentAccountMigrator.Migrate(data);

        preExisting.Aliases.Should().BeEquivalentTo("Chase save");
    }

    [Fact]
    public void Migrate_SnapshotWithMatchingAccountName_CountsAsResolvedAndLeavesValueUntouched()
    {
        var data = CashFlowData.Create();
        var snapshot = InvestmentSnapshot.Create("ChaseSave", 2026, 7, 500m);
        data.AddInvestmentSnapshot(snapshot);

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.SnapshotsResolvedCount.Should().Be(1);
        summary.UnresolvedSnapshots.Should().BeEmpty();
        snapshot.Account.Should().Be("ChaseSave");
        snapshot.Value.Should().Be(500m);
    }

    [Fact]
    public void Migrate_SnapshotWithUnresolvableAccountName_IsFlaggedForManualReviewAndLeftUntouched()
    {
        var data = CashFlowData.Create();
        var snapshot = InvestmentSnapshot.Create("SomeUnknownAccount", 2020, 7, 250m);
        data.AddInvestmentSnapshot(snapshot);

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.UnresolvedSnapshots.Should().ContainSingle().Which.Id.Should().Be(snapshot.Id);
        summary.SnapshotsResolvedCount.Should().Be(0);
        snapshot.Account.Should().Be("SomeUnknownAccount");
        snapshot.Value.Should().Be(250m);
    }

    [Fact]
    public void Migrate_SnapshotWithHistoricalAccountName_NowCountsAsResolved()
    {
        var data = CashFlowData.Create();
        var snapshot = InvestmentSnapshot.Create("EverydaySaver", 2020, 7, 250m);
        data.AddInvestmentSnapshot(snapshot);

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.SnapshotsResolvedCount.Should().Be(1);
        summary.UnresolvedSnapshots.Should().BeEmpty();
    }

    [Fact]
    public void Migrate_SecondRunOverFirstRunsOutput_ChangesNothing()
    {
        var data = CashFlowData.Create();
        var resolved = InvestmentSnapshot.Create("ChaseSave", 2026, 7, 500m);
        var unresolved = InvestmentSnapshot.Create("SomeUnknownAccount", 2020, 7, 250m);
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
