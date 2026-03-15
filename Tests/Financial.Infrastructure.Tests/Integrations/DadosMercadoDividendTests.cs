using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Common;
using Financial.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;
using HtmlAgilityPack;

namespace Financial.Infrastructure.Tests.Integrations;

public class DadosMercadoDividendTests
{
    [Fact]
    public void ParseDividendRow_WhenDividendo_ReturnsDividend()
    {
        var cells = BuildCells("<tr><td>Dividendo</td><td>1,23</td><td>x</td><td>y</td><td>2024-03-01</td></tr>");

        var result = DadosMercadoDividend.ParseDividendRow(cells);

        result.Type.Should().Be(DividendType.Dividend);
        result.Value.Should().Be(1.23m);
        result.Date.Should().Be(new DateTime(2024, 3, 1));
    }

    [Fact]
    public void ParseDividendRow_WhenNotDividendo_ReturnsJcp()
    {
        var cells = BuildCells("<tr><td>JCP</td><td>2,00</td><td>x</td><td>y</td><td>2024-04-02</td></tr>");

        var result = DadosMercadoDividend.ParseDividendRow(cells);

        result.Type.Should().Be(DividendType.JCP);
        result.Value.Should().Be(2.00m);
        result.Date.Should().Be(new DateTime(2024, 4, 2));
    }

    private static IReadOnlyList<HtmlNode> BuildCells(string rowHtml)
    {
        var document = new HtmlDocument();
        document.LoadHtml(rowHtml);
        var row = document.DocumentNode.SelectSingleNode("//tr");
        return row.SelectNodes("th|td").ToList();
    }
}
