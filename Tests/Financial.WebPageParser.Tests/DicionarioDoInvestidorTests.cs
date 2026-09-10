using Financial.Integrations.WebPageParser;
using FluentAssertions;
using HtmlAgilityPack;

namespace Financial.WebPageParser.Tests;

public class DicionarioDoInvestidorTests
{
    [Fact]
    public void FindCurrentValue_BondListed_ReturnsValorAtual()
    {
        var htmlDoc = LoadTitulosTable(
            ("Tesouro IPCA+ 2040", "R$ 1.755,91"),
            ("Tesouro Selic 2031", "R$ 19.771,57"));

        var price = DicionarioDoInvestidor.FindCurrentValue(htmlDoc, "TESOURO IPCA+ 2040");

        price.Should().Be(1755.91m);
    }

    [Fact]
    public void FindCurrentValue_MatchIsCaseInsensitive()
    {
        var htmlDoc = LoadTitulosTable(("Tesouro Selic 2031", "R$ 19.771,57"));

        var price = DicionarioDoInvestidor.FindCurrentValue(htmlDoc, "tesouro selic 2031");

        price.Should().Be(19771.57m);
    }

    [Fact]
    public void FindCurrentValue_BondNotListed_ReturnsNull()
    {
        var htmlDoc = LoadTitulosTable(("Tesouro IPCA+ 2040", "R$ 1.755,91"));

        var price = DicionarioDoInvestidor.FindCurrentValue(htmlDoc, "TESOURO IPCA+ 2029");

        price.Should().BeNull();
    }

    [Fact]
    public void FindCurrentValue_NoTableInPage_ReturnsNull()
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml("<html><body>Some unrelated page content.</body></html>");

        var price = DicionarioDoInvestidor.FindCurrentValue(htmlDoc, "TESOURO IPCA+ 2040");

        price.Should().BeNull();
    }

    private static HtmlDocument LoadTitulosTable(params (string Name, string ValorAtual)[] rows)
    {
        var rowsHtml = string.Join(string.Empty, rows.Select(r => $"""
            <tr>
                <td class="font-bold"><a href="titulo.php?nome=x">{r.Name}</a></td>
                <td class="text-right font-mono">{r.ValorAtual}</td>
                <td class="text-center">0,00%</td>
            </tr>
            """));

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml($"""
            <html><body><table>
                <tr><th>Título</th><th>Valor Atual</th><th>Oscilação</th></tr>
                {rowsHtml}
            </table></body></html>
            """);
        return htmlDoc;
    }
}
