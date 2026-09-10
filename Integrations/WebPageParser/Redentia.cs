using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Financial.Integrations.WebPageParser;

/// <summary>
/// Parses redentia.com.br's per-bond page as a second fallback source, for when both Status
/// Invest and dicionariodoinvestidor.com fail. Its "Compra" card - "Quanto custa investir neste
/// titulo hoje, pelo preco unitario do Tesouro Nacional" - reports the same market value Status
/// Invest exposes as "Valor de Venda" (confirmed against concurrent fetches; see
/// DicionarioDoInvestidor for the same cross-site labeling note - both dicionariodoinvestidor.com
/// and this site call it "Compra" where Status Invest calls it "Venda").
/// Unlike dicionariodoinvestidor.com, this site lists every Tesouro Direto series including
/// matured/no-longer-offered ones, and uses the same URL slug shape as Status Invest.
/// </summary>
public static class Redentia
{
    private const string BaseUrl = "https://www.redentia.com.br/tesouro/";

    public static WebAssetQuote GetSellValue(string bondTitle)
    {
        var slug = TesouroDiretoSlug.Derive(bondTitle);
        var url = BaseUrl + slug;

        HtmlDocument htmlDoc;
        HtmlWeb htmlWeb;
        try
        {
            htmlWeb = new HtmlWeb();
            htmlDoc = htmlWeb.Load(url);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not load redentia.com.br page for '{bondTitle}' ({url}).", ex);
        }

        if (htmlWeb.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"redentia.com.br returned {(int)htmlWeb.StatusCode} for '{bondTitle}' ({url}).");
        }

        var price = ExtractBuyPrice(htmlDoc)
            ?? throw new InvalidOperationException($"'Compra' price not found for '{bondTitle}' on redentia.com.br. The page structure may have changed.");

        return new WebAssetQuote(bondTitle, bondTitle, price, DateTimeOffset.Now);
    }

    internal static decimal? ExtractBuyPrice(HtmlDocument htmlDoc)
    {
        var labelNode = htmlDoc.DocumentNode
            .SelectNodes("//p[@class='tsc__label']")
            ?.FirstOrDefault(node => node.InnerText.Trim() == "Compra");

        var priceNode = labelNode?.SelectSingleNode("following-sibling::p[@class='tsc__price'][1]");
        if (priceNode is null)
        {
            return null;
        }

        var match = Regex.Match(priceNode.InnerText, @"R\$\s*([\d.,]+)");
        return match.Success ? StatusInvest.ParsePrice(match.Groups[1].Value) : null;
    }
}
