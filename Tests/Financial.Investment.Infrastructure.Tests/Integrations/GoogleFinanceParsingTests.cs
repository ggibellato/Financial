using Financial.Investment.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

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

    [Fact]
    public void TryParseAsOf_WithoutUtcOffset_FallsBackToExactFormatArrayAndParses()
    {
        var result = GoogleFinanceParsing.TryParseAsOf("As of Sep 1, 3:45:00 PM");

        result.Should().NotBeNull();
        result!.Value.Month.Should().Be(9);
        result.Value.Day.Should().Be(1);
        result.Value.Hour.Should().Be(15);
        result.Value.Minute.Should().Be(45);
    }

    [Fact]
    public void TryParseAsOf_Iso8601Style_FallsBackToInvariantCultureParse()
    {
        var result = GoogleFinanceParsing.TryParseAsOf("2026-09-01 15:45:00");

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2026);
        result.Value.Month.Should().Be(9);
        result.Value.Day.Should().Be(1);
        result.Value.Hour.Should().Be(15);
        result.Value.Minute.Should().Be(45);
    }

    [Fact]
    public void TryParseAsOf_UnparseableGarbage_ReturnsNull()
    {
        var result = GoogleFinanceParsing.TryParseAsOf("not a date at all");

        result.Should().BeNull();
    }
}
