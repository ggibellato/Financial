using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class SheetNameParserTests
{
    [Theory]
    [InlineData("Jul2026", 2026, 7)]
    [InlineData("Fev2017", 2017, 2)]
    [InlineData("Dez2025", 2025, 12)]
    [InlineData("Jan2019", 2019, 1)]
    public void TryParseMonthlySheetName_AbbreviatedNames_ParsesCorrectly(string sheetName, int expectedYear, int expectedMonth)
    {
        var result = SheetNameParser.TryParseMonthlySheetName(sheetName, out var year, out var month);

        result.Should().BeTrue();
        year.Should().Be(expectedYear);
        month.Should().Be(expectedMonth);
    }

    [Theory]
    [InlineData("Janeiro 2017")]
    [InlineData("Julho 2014")]
    [InlineData("Resumo2026")]
    [InlineData("Reservas")]
    [InlineData("Mensais")]
    [InlineData("Controle mae")]
    [InlineData("Media Anual")]
    [InlineData("Casa")]
    public void TryParseMonthlySheetName_NonMonthlyOrLegacyNames_ReturnsFalse(string sheetName)
    {
        var result = SheetNameParser.TryParseMonthlySheetName(sheetName, out _, out _);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(2017, 2, true)]
    [InlineData(2017, 1, false)]
    [InlineData(2020, 6, true)]
    [InlineData(2026, 8, true)]
    [InlineData(2027, 1, false)]
    public void IsInScope_MatchesFebruary2017Through2026(int year, int month, bool expected)
    {
        SheetNameParser.IsInScope(year, month).Should().Be(expected);
    }
}
