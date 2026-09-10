using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Financial.Integrations.WebPageParser;

/// <summary>
/// Parses dicionariodoinvestidor.com's Tesouro Direto bond list as a fallback source for when
/// Status Invest is unreachable or blocked. The site's "Valor Atual" column lines up with what
/// <see cref="StatusInvest.GetSellValue"/> extracts as Status Invest's "Valor de Venda" - the two
/// sites label buy/sell from opposite perspectives, but both report the same market value for a
/// given bond (confirmed against concurrent fetches from both sites).
/// Only bonds Tesouro Direto is currently offering for sale are listed here - a matured or
/// no-longer-offered series that is still held returns no match, which is why this is a fallback
/// alongside Status Invest rather than a replacement for it.
/// </summary>
public static class DicionarioDoInvestidor
{
    private const string TitulosUrl = "https://dicionariodoinvestidor.com/public/titulos.php";

    public static WebAssetQuote GetSellValue(string bondTitle)
    {
        HtmlDocument htmlDoc;
        HtmlWeb htmlWeb;
        try
        {
            htmlWeb = new HtmlWeb();
            htmlDoc = htmlWeb.Load(TitulosUrl);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not load dicionariodoinvestidor.com's bond list for '{bondTitle}'.", ex);
        }

        if (htmlWeb.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"dicionariodoinvestidor.com returned {(int)htmlWeb.StatusCode} while looking up '{bondTitle}'.");
        }

        var price = FindCurrentValue(htmlDoc, bondTitle)
            ?? throw new InvalidOperationException(
                $"'{bondTitle}' not found in dicionariodoinvestidor.com's bond list. It may not be currently offered by Tesouro Direto, or the page structure may have changed.");

        return new WebAssetQuote(bondTitle, bondTitle, price, DateTimeOffset.Now);
    }

    internal static decimal? FindCurrentValue(HtmlDocument htmlDoc, string bondTitle)
    {
        var nameLink = htmlDoc.DocumentNode
            .SelectNodes("//table//tr/td/a")
            ?.FirstOrDefault(a => string.Equals(NormalizeWhitespace(a.InnerText), bondTitle.Trim(), StringComparison.OrdinalIgnoreCase));

        var priceCell = nameLink?.ParentNode?.ParentNode?.SelectSingleNode("td[2]");
        if (priceCell is null)
        {
            return null;
        }

        var match = Regex.Match(priceCell.InnerText, @"R\$\s*([\d.,]+)");
        return match.Success ? StatusInvest.ParsePrice(match.Groups[1].Value) : null;
    }

    private static string NormalizeWhitespace(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}
