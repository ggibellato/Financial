using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.IncomeSources;

[Trait("Category", "Unit")]
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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
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
        var bank = TestAccounts.Bank("Chase", roundUpEnabled: true);
        var income = Income.Create(new DateOnly(2026, 7, 1), notASource, null, 10m, bank);
        data.AddIncome(income);

        var summary = IncomeSourceMigrator.Migrate(data);

        summary.UnresolvedIncomes.Should().ContainSingle().Which.Id.Should().Be(income.Id);
        summary.IncomesResolvedCount.Should().Be(0);
        income.IncomeSource.Should().Be(notASource);
    }

    [Fact]
    public void Migrate_WithReserva_SeedsArianaSplittingToReserva()
    {
        var data = CashFlowData.Create();
        var reserva = TestAccounts.ReservaIn(data);

        IncomeSourceMigrator.Migrate(data);

        data.IncomeSources.Should().ContainSingle(s => s.Name == "Ariana").Which.PotsAccount.Should().BeSameAs(reserva);
    }

    [Fact]
    public void Migrate_WithReserva_SeedsOtherSourcesWithoutPotsAccount()
    {
        var data = CashFlowData.Create();
        TestAccounts.ReservaIn(data);

        IncomeSourceMigrator.Migrate(data);

        data.IncomeSources.Where(s => s.Name != "Ariana").Should().OnlyContain(s => s.PotsAccount == null);
    }

    [Fact]
    public void Migrate_WithoutPotsAccount_SeedsArianaWithoutPotsAccount()
    {
        var data = CashFlowData.Create();

        IncomeSourceMigrator.Migrate(data);

        data.IncomeSources.Should().ContainSingle(s => s.Name == "Ariana").Which.PotsAccount.Should().BeNull();
    }

    [Fact]
    public void Migrate_WithArianaAlreadyPresentWithoutPotsAccount_LeavesExistingRecordUnchanged()
    {
        var data = CashFlowData.Create();
        TestAccounts.ReservaIn(data);
        var preExistingAriana = IncomeSource.Create("Ariana", IncomeGroup.Salary);
        data.AddIncomeSource(preExistingAriana);

        var summary = IncomeSourceMigrator.Migrate(data);

        summary.SourcesAlreadyPresentCount.Should().Be(1);
        data.IncomeSources.Should().ContainSingle(s => s.Name == "Ariana").Which.Should().BeSameAs(preExistingAriana);
        preExistingAriana.PotsAccount.Should().BeNull();
    }
}
