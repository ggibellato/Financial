using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.Categories;

public class CategoryMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllFourteenCategoriesActive()
    {
        var data = CashFlowData.Create();

        var summary = CategoryMigrator.Migrate(data);

        summary.CategoriesSeededCount.Should().Be(14);
        summary.CategoriesAlreadyPresentCount.Should().Be(0);
        data.Categories.Should().HaveCount(14);
        data.Categories.Should().OnlyContain(c => c.Active);
    }

    [Fact]
    public void Migrate_OnEmptyData_OnlyInvestimentoHasIsInvestmentTrue()
    {
        var data = CashFlowData.Create();

        CategoryMigrator.Migrate(data);

        data.Categories.Should().ContainSingle(c => c.IsInvestment).Which.Name.Should().Be("Investimento");
    }

    [Fact]
    public void Migrate_OnEmptyData_OnlyDizimoHasIsTitheTrue()
    {
        var data = CashFlowData.Create();

        CategoryMigrator.Migrate(data);

        data.Categories.Should().ContainSingle(c => c.IsTithe).Which.Name.Should().Be("Dizimo");
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRunAndKeepsSameIds()
    {
        var data = CashFlowData.Create();
        CategoryMigrator.Migrate(data);
        var idsAfterFirstRun = data.Categories.Select(c => c.Id).OrderBy(id => id).ToList();

        var secondSummary = CategoryMigrator.Migrate(data);

        secondSummary.CategoriesSeededCount.Should().Be(0);
        secondSummary.CategoriesAlreadyPresentCount.Should().Be(14);
        data.Categories.Should().HaveCount(14);
        data.Categories.Select(c => c.Id).OrderBy(id => id).Should().Equal(idsAfterFirstRun);
    }

    [Fact]
    public void Migrate_WithSomeCategoriesAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddCategory(Category.Create("Mercado"));

        var summary = CategoryMigrator.Migrate(data);

        summary.CategoriesSeededCount.Should().Be(13);
        summary.CategoriesAlreadyPresentCount.Should().Be(1);
        data.Categories.Should().HaveCount(14);
    }

    [Fact]
    public void Migrate_WithNullData_Throws()
    {
        var act = () => CategoryMigrator.Migrate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
