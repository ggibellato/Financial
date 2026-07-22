using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class CategoryResolverTests
{
    [Fact]
    public void TryResolve_KnownCategoryName_ReturnsTrue()
    {
        var result = CategoryResolver.TryResolve("Mercado", out var category);

        result.Should().BeTrue();
        category.Should().Be(Category.Mercado);
    }

    [Fact]
    public void TryResolve_KnownHistoricalTypo_Casas_ResolvesToCasa()
    {
        var result = CategoryResolver.TryResolve("Casas", out var category);

        result.Should().BeTrue();
        category.Should().Be(Category.Casa);
    }

    [Fact]
    public void TryResolve_UnknownLabel_ReturnsFalse()
    {
        var result = CategoryResolver.TryResolve("NotACategory", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_BlankLabel_ReturnsFalse()
    {
        var result = CategoryResolver.TryResolve(null, out _);

        result.Should().BeFalse();
    }
}
