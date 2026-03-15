using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using FluentAssertions;
using Google.Apis.Sheets.v4.Data;

namespace Financial.Infrastructure.Tests.Integrations;

public class GoogleSheetValueParserTests
{
    [Fact]
    public void ToDecimal_WithExtendedValue_ReturnsNumberValue()
    {
        var value = new ExtendedValue { NumberValue = 12.5 };

        var result = GoogleSheetValueParser.ToDecimal(value);

        result.Should().Be(12.5m);
    }

    [Fact]
    public void ToDecimal_WithString_ReturnsParsedDecimal()
    {
        var result = GoogleSheetValueParser.ToDecimal("1,000");

        result.Should().Be(1000m);
    }
}
