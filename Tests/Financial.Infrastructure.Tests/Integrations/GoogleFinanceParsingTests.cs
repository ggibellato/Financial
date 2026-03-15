using System;
using Financial.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Integrations;

public class GoogleFinanceParsingTests
{
    [Fact]
    public void ParsePriceValue_WithCurrencySymbol_ReturnsExpectedValue()
    {
        var result = GoogleFinanceParsing.ParsePriceValue("R$ 100");

        result.Should().Be(100m);
    }

    [Fact]
    public void ParsePriceValue_WithGbxValue_ScalesDown()
    {
        var result = GoogleFinanceParsing.ParsePriceValue("GBX100");

        result.Should().Be(1m);
    }

    [Fact]
    public void TryParseAsOf_WhenValueIsNull_ReturnsNull()
    {
        var result = GoogleFinanceParsing.TryParseAsOf(null);

        result.Should().BeNull();
    }

    [Fact]
    public void TryParseAsOf_WithUtcOffset_ReturnsParsedValue()
    {
        var result = GoogleFinanceParsing.TryParseAsOf("As of Sep 1, 3:45:00 PM UTC+1");

        result.Should().NotBeNull();
        result!.Value.Offset.Should().Be(TimeSpan.FromHours(1));
        result.Value.Month.Should().Be(9);
        result.Value.Day.Should().Be(1);
        result.Value.Hour.Should().Be(15);
    }
}
