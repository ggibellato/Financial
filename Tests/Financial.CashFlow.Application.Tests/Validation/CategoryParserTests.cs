using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CategoryParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = CategoryParser.TryParse("Mercado", out var category);

        result.Should().BeTrue();
        category.Should().Be(Category.Mercado);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = CategoryParser.TryParse("NotACategory", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = CategoryParser.TryParse("  ", out _);

        result.Should().BeFalse();
    }
}
