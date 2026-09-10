using Financial.Integrations.WebPageParser;
using FluentAssertions;
using HtmlAgilityPack;

namespace Financial.WebPageParser.Tests;

public class RedentiaTests
{
    [Fact]
    public void ExtractBuyPrice_CompraCardPresent_ReturnsItsPrice()
    {
        var htmlDoc = LoadBondPage(compra: "R$ 955,15", venda: "R$ 828,68");

        var price = Redentia.ExtractBuyPrice(htmlDoc);

        price.Should().Be(955.15m);
    }

    [Fact]
    public void ExtractBuyPrice_IgnoresVendaCard()
    {
        var htmlDoc = LoadBondPage(compra: "R$ 1.755,91", venda: "R$ 1.783,30");

        var price = Redentia.ExtractBuyPrice(htmlDoc);

        price.Should().Be(1755.91m);
    }

    [Fact]
    public void ExtractBuyPrice_NoCompraCard_ReturnsNull()
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml("<html><body>Some unrelated page content.</body></html>");

        var price = Redentia.ExtractBuyPrice(htmlDoc);

        price.Should().BeNull();
    }

    /// <summary>
    /// Body copy elsewhere on the page uses lower-case "compra" in ordinary sentences; only the
    /// price card's own label element should be matched.
    /// </summary>
    [Fact]
    public void ExtractBuyPrice_LowercaseCompraElsewhereOnPage_StillFindsTheCard()
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml("""
            <html><body>
                <p>Saiba como funciona a compra e venda de titulos do Tesouro Direto.</p>
                <section class="tsc__grid">
                    <article><p class="tsc__label">Compra</p><p class="tsc__price">R$ 955,15</p></article>
                    <article><p class="tsc__label">Venda</p><p class="tsc__price">R$ 828,68</p></article>
                </section>
            </body></html>
            """);

        var price = Redentia.ExtractBuyPrice(htmlDoc);

        price.Should().Be(955.15m);
    }

    private static HtmlDocument LoadBondPage(string compra, string venda)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml($"""
            <html><body><section class="tsc__grid">
                <article><p class="tsc__label">Compra</p><p class="tsc__price">{compra}</p></article>
                <article><p class="tsc__label">Venda</p><p class="tsc__price">{venda}</p></article>
            </section></body></html>
            """);
        return htmlDoc;
    }
}
