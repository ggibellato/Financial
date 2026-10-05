using Financial.Tests;
using Financial.Integrations.GoogleSheets;
using FluentAssertions;
using Google.Apis.Sheets.v4.Data;

namespace Financial.GoogleIntegrations.Tests;

[Trait("Category", "Unit")]
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

    [Fact]
    public void ToDecimal_ExtendedValueWithNullNumberValue_FallsThroughToStringParsingAndThrows()
    {
        // ExtendedValue.ToString() does not surface StringValue (it isn't overridden), so this
        // fallback path never actually recovers a numeric string from an ExtendedValue - it
        // always throws. This pins that real, if surprising, behavior.
        var value = new ExtendedValue { NumberValue = null, StringValue = "1,000" };

        var act = () => GoogleSheetValueParser.ToDecimal(value);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("65.637", 65.637)]
    [InlineData("-656.37", -656.37)]
    [InlineData("1,234.50", 1234.50)]
    [UseCulture("pt-BR")]
    public void ToDecimal_WithMachineFormattedString_ParsesInvariantlyWhateverTheHostCulture(string cell, double expected)
    {
        GoogleSheetValueParser.ToDecimal(cell).Should().Be((decimal)expected);
    }
}
