using Financial.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;
using HtmlAgilityPack;

namespace Financial.Infrastructure.Tests.Integrations;

public class TesouroDiretoTests
{
    [Fact]
    public void FindMatchingRow_ExactTitleMatch_ReturnsRow()
    {
        var table = BuildTable(
            "<table><thead><tr><th>Título</th><th>Preço Unit.</th></tr></thead>" +
            "<tbody><tr><td>TESOURO IPCA+ 2029</td><td>R$ 3.775,97</td></tr></tbody></table>");

        var result = TesouroDireto.FindMatchingRow(table, "TESOURO IPCA+ 2029");

        result.Should().NotBeNull();
        result!.Name.Should().Be("TESOURO IPCA+ 2029");
        result.Price.Should().Be(3775.97m);
    }

    [Fact]
    public void FindMatchingRow_CaseInsensitiveMatch_ReturnsRow()
    {
        var table = BuildTable(
            "<table><thead><tr><th>Título</th><th>Preço Unit.</th></tr></thead>" +
            "<tbody><tr><td>TESOURO IPCA+ 2029</td><td>R$ 3.775,97</td></tr></tbody></table>");

        var result = TesouroDireto.FindMatchingRow(table, "tesouro ipca+ 2029");

        result.Should().NotBeNull();
        result!.Price.Should().Be(3775.97m);
    }

    [Fact]
    public void FindMatchingRow_WhitespaceDifference_ReturnsRow()
    {
        var table = BuildTable(
            "<table><thead><tr><th>Título</th><th>Preço Unit.</th></tr></thead>" +
            "<tbody><tr><td>  TESOURO SELIC 2029  </td><td>R$ 15.234,10</td></tr></tbody></table>");

        var result = TesouroDireto.FindMatchingRow(table, "TESOURO SELIC 2029");

        result.Should().NotBeNull();
        result!.Price.Should().Be(15234.10m);
    }

    [Fact]
    public void FindMatchingRow_NoMatch_ReturnsNull()
    {
        var table = BuildTable(
            "<table><thead><tr><th>Título</th><th>Preço Unit.</th></tr></thead>" +
            "<tbody><tr><td>TESOURO IPCA+ 2029</td><td>R$ 3.775,97</td></tr></tbody></table>");

        var result = TesouroDireto.FindMatchingRow(table, "TESOURO PREFIXADO 2027");

        result.Should().BeNull();
    }

    private static HtmlNode BuildTable(string tableHtml)
    {
        var document = new HtmlDocument();
        document.LoadHtml(tableHtml);
        return document.DocumentNode.SelectSingleNode("//table");
    }
}
