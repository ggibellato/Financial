using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class CategoryResolverTests
{
    private static readonly Dictionary<string, Category> CategoriesByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mercado"] = Category.Create("Mercado"),
        ["Casa"] = Category.Create("Casa"),
    };

    [Fact]
    public void TryResolve_KnownCategoryName_ReturnsTrue()
    {
        var result = CategoryResolver.TryResolve("Mercado", CategoriesByName, out var category);

        result.Should().BeTrue();
        category.Should().BeSameAs(CategoriesByName["Mercado"]);
    }

    [Fact]
    public void TryResolve_KnownHistoricalTypo_Casas_ResolvesToCasa()
    {
        var result = CategoryResolver.TryResolve("Casas", CategoriesByName, out var category);

        result.Should().BeTrue();
        category.Should().BeSameAs(CategoriesByName["Casa"]);
    }

    [Fact]
    public void TryResolve_UnknownLabel_ReturnsFalse()
    {
        var result = CategoryResolver.TryResolve("NotACategory", CategoriesByName, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_BlankLabel_ReturnsFalse()
    {
        var result = CategoryResolver.TryResolve(null, CategoriesByName, out _);

        result.Should().BeFalse();
    }
}
