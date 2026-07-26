using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.InvestmentAccounts;

public class InvestmentAccountMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllElevenAccountsWithCorrectLiabilityFlags()
    {
        var data = CashFlowData.Create();

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.AccountsSeededCount.Should().Be(11);
        summary.AccountsAlreadyPresentCount.Should().Be(0);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "PlatinumVisa8003" && a.IsLiability);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ReservasPessoais" && a.IsLiability);
        data.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChaseSave" && !a.IsLiability);
        data.InvestmentAccounts.Should().OnlyContain(a => a.IsActive);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRun()
    {
        var data = CashFlowData.Create();
        InvestmentAccountMigrator.Migrate(data);

        var secondSummary = InvestmentAccountMigrator.Migrate(data);

        secondSummary.AccountsSeededCount.Should().Be(0);
        secondSummary.AccountsAlreadyPresentCount.Should().Be(11);
        data.InvestmentAccounts.Should().HaveCount(11);
    }

    [Fact]
    public void Migrate_WithSomeAccountsAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.AccountsSeededCount.Should().Be(10);
        summary.AccountsAlreadyPresentCount.Should().Be(1);
        data.InvestmentAccounts.Should().HaveCount(11);
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
        var snapshot = InvestmentSnapshot.Create("EverydaySaver", 2020, 7, 250m);
        data.AddInvestmentSnapshot(snapshot);

        var summary = InvestmentAccountMigrator.Migrate(data);

        summary.UnresolvedSnapshots.Should().ContainSingle().Which.Id.Should().Be(snapshot.Id);
        summary.SnapshotsResolvedCount.Should().Be(0);
        snapshot.Account.Should().Be("EverydaySaver");
        snapshot.Value.Should().Be(250m);
    }

    [Fact]
    public void Migrate_SecondRunOverFirstRunsOutput_ChangesNothing()
    {
        var data = CashFlowData.Create();
        var resolved = InvestmentSnapshot.Create("ChaseSave", 2026, 7, 500m);
        var unresolved = InvestmentSnapshot.Create("EverydaySaver", 2020, 7, 250m);
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
