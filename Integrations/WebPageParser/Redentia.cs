using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Financial.Integrations.WebPageParser;

/// <summary>
/// Parses redentia.com.br's per-bond page as a last fallback source, for when Status Invest and
/// the Tesouro Transparente price list both fail. Its "Compra" card reports the same market value
/// Status Invest exposes as "Valor de Venda" - the two sites label buy and sell from opposite
/// perspectives (confirmed against concurrent fetches).
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
