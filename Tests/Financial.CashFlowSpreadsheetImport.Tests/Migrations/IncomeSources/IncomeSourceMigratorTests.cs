using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.IncomeSources;

public class IncomeSourceMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllFourIncomeSourcesWithCorrectGroups()
    {
        var data = CashFlowData.Create();

        var summary = IncomeSourceMigrator.Migrate(data);

        summary.SourcesSeededCount.Should().Be(4);
        summary.SourcesAlreadyPresentCount.Should().Be(0);
        data.IncomeSources.Should().ContainSingle(s => s.Name == "Gleison" && s.Group == IncomeGroup.Salary && s.IsActive);
        data.IncomeSources.Should().ContainSingle(s => s.Name == "Ariana" && s.Group == IncomeGroup.Salary && s.IsActive);
        data.IncomeSources.Should().ContainSingle(s => s.Name == "Lottery" && s.Group == IncomeGroup.NonReportable && s.IsActive);
        data.IncomeSources.Should().ContainSingle(s => s.Name == "DividendoJuros" && s.Group == IncomeGroup.DividendoJuros && s.IsActive);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRunAndKeepsSameIds()
    {
        var data = CashFlowData.Create();
        IncomeSourceMigrator.Migrate(data);
        var idsAfterFirstRun = data.IncomeSources.Select(s => s.Id).OrderBy(id => id).ToList();

        var secondSummary = IncomeSourceMigrator.Migrate(data);

        secondSummary.SourcesSeededCount.Should().Be(0);
        secondSummary.SourcesAlreadyPresentCount.Should().Be(4);
        data.IncomeSources.Should().HaveCount(4);
        data.IncomeSources.Select(s => s.Id).OrderBy(id => id).Should().Equal(idsAfterFirstRun);
    }

    [Fact]
    public void Migrate_WithSomeSourcesAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));

        var summary = IncomeSourceMigrator.Migrate(data);

        summary.SourcesSeededCount.Should().Be(3);
        summary.SourcesAlreadyPresentCount.Should().Be(1);
        data.IncomeSources.Should().HaveCount(4);
    }

    [Fact]
    public void Migrate_IncomeWithMatchingSourceName_CountsAsResolvedAndLeavesValueUntouched()
    {
        var data = CashFlowData.Create();
        var gleison = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        data.AddIncomeSource(gleison);
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var income = Income.Create(new DateOnly(2026, 7, 1), gleison, 3200m, 2450m, bank);
        data.AddIncome(income);

        var summary = IncomeSourceMigrator.Migrate(data);

        summary.IncomesResolvedCount.Should().Be(1);
        summary.UnresolvedIncomes.Should().BeEmpty();
        income.IncomeSource.Should().Be(gleison);
    }

    [Fact]
    public void Migrate_IncomeWithUnresolvableSourceName_IsFlaggedForManualReviewAndLeftUntouched()
    {
        var data = CashFlowData.Create();
        var notASource = IncomeSource.Create("NotASource", IncomeGroup.NonReportable);
        var bank = Bank.Create("Chase", roundUpEnabled: true);
        var income = Income.Create(new DateOnly(2026, 7, 1), notASource, null, 10m, bank);
        data.AddIncome(income);

        var summary = IncomeSourceMigrator.Migrate(data);

        summary.UnresolvedIncomes.Should().ContainSingle().Which.Id.Should().Be(income.Id);
        summary.IncomesResolvedCount.Should().Be(0);
        income.IncomeSource.Should().Be(notASource);
    }

    [Fact]
    public void Migrate_WithNullData_Throws()
    {
        var act = () => IncomeSourceMigrator.Migrate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
