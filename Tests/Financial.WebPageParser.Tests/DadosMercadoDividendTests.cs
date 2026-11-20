using Financial.Integrations.WebPageParser;
using FluentAssertions;
using HtmlAgilityPack;

namespace Financial.WebPageParser.Tests;

[Trait("Category", "Unit")]
public class DadosMercadoDividendTests
{
    [Fact]
    public void ParseDividendRow_WhenDividendo_ReturnsDividend()
    {
        var cells = BuildCells("<tr><td>Dividendo</td><td>1,23</td><td>x</td><td>y</td><td>01/03/2024</td></tr>");

        var result = DadosMercadoDividend.ParseDividendRow(cells);

        result.Type.Should().Be(WebDividendType.Dividend);
        result.Value.Should().Be(1.23m);
        result.Date.Should().Be(new DateTime(2024, 3, 1));
    }

    [Fact]
    public void ParseDividendRow_WhenNotDividendo_ReturnsJcp()
    {
        var cells = BuildCells("<tr><td>JCP</td><td>2,00</td><td>x</td><td>y</td><td>02/04/2024</td></tr>");

        var result = DadosMercadoDividend.ParseDividendRow(cells);

        result.Type.Should().Be(WebDividendType.JCP);
        result.Value.Should().Be(2.00m);
        result.Date.Should().Be(new DateTime(2024, 4, 2));
    }

    [Fact]
    public void ParseDividendRow_ValueWithFootnoteMarker_StripsMarkerBeforeParsing()
    {
        var cells = BuildCells("<tr><td>Dividendo</td><td>1,23* </td><td>x</td><td>y</td><td>01/03/2024</td></tr>");

        var result = DadosMercadoDividend.ParseDividendRow(cells);

        result.Value.Should().Be(1.23m);
    }

    [Fact]
    public void ParseDividendRow_DateWithLeadingWhitespace_IsTrimmedAndParsed()
    {
        var cells = BuildCells("<tr><td>Dividendo</td><td>1,00</td><td>x</td><td>y</td><td>  19/08/2026  </td></tr>");

        var result = DadosMercadoDividend.ParseDividendRow(cells);

        result.Date.Should().Be(new DateTime(2026, 8, 19));
    }

    [Fact]
    public void ParseDividendRow_RowWithFewerThanFiveColumns_IsRefused()
    {
        var cells = BuildCells("<tr><td>Dividendo</td><td>1,00</td><td>x</td><td>19/08/2026</td></tr>");

        var act = () => DadosMercadoDividend.ParseDividendRow(cells);

        act.Should().Throw<InvalidOperationException>().WithMessage("Dividend row does not contain expected columns.");
    }

    [Theory]
    [InlineData("abc", "19/08/2026")]
    [InlineData("1,00", "2026-08-19")]
    public void ParseDividendRow_UnreadableValueOrDate_IsRefused(string value, string date)
    {
        var cells = BuildCells($"<tr><td>Dividendo</td><td>{value}</td><td>x</td><td>y</td><td>{date}</td></tr>");

        var act = () => DadosMercadoDividend.ParseDividendRow(cells);

        act.Should().Throw<FormatException>();
    }

    private static IReadOnlyList<HtmlNode> BuildCells(string rowHtml)
    {
        var document = new HtmlDocument();
        document.LoadHtml(rowHtml);
        var row = document.DocumentNode.SelectSingleNode("//tr");
        return row.SelectNodes("th|td").ToList();
    }
}
