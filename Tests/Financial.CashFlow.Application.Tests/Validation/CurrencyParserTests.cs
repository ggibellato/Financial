using Financial.CashFlow.Application.Validation;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CurrencyParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = CurrencyParser.TryParse("GBP", out var currency);

        result.Should().BeTrue();
        currency.Should().Be(Currency.GBP);
    }

    [Fact]
    public void TryParse_Usd_ReturnsTrueAndParsedValue()
    {
        var result = CurrencyParser.TryParse("USD", out var currency);

        result.Should().BeTrue();
        currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = CurrencyParser.TryParse("EUR", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = CurrencyParser.TryParse(null, out _);

        result.Should().BeFalse();
    }
}
