using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class IncomeSourceParserTests
{
    [Theory]
    [InlineData("Gleison", IncomeSource.Gleison)]
    [InlineData("Ariana", IncomeSource.Ariana)]
    [InlineData("Lottery", IncomeSource.Lottery)]
    [InlineData("DividendoJuros", IncomeSource.DividendoJuros)]
    public void TryParse_ValidValue_ReturnsTrueAndTheSource(string value, IncomeSource expected)
    {
        var result = IncomeSourceParser.TryParse(value, out var incomeSource);

        result.Should().BeTrue();
        incomeSource.Should().Be(expected);
    }

    [Fact]
    public void TryParse_DifferentCasing_ResolvesCaseInsensitively()
    {
        var result = IncomeSourceParser.TryParse("gleison", out var incomeSource);

        result.Should().BeTrue();
        incomeSource.Should().Be(IncomeSource.Gleison);
    }

    [Fact]
    public void TryParse_UnrecognizedValue_ReturnsFalse()
    {
        var result = IncomeSourceParser.TryParse("NotASource", out _);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryParse_NullOrEmptyValue_ReturnsFalse(string? value)
    {
        var result = IncomeSourceParser.TryParse(value, out _);

        result.Should().BeFalse();
    }
}
