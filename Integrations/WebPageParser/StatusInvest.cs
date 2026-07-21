using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Financial.Investment.Domain.ValueObjects;
using HtmlAgilityPack;

namespace Financial.Infrastructure.Integrations.WebPageParser;

/// <summary>
/// Parses Status Invest's per-bond page for the "Valor de Venda" (sell price).
/// The real page markup could not be inspected for CSS classes/IDs while this
/// parser was written, so it extracts by text pattern rather than selectors.
/// Run StatusInvestVerificationTests manually to confirm it still matches production.
/// </summary>
public static class StatusInvest
{
    private const string BaseUrl = "https://statusinvest.com.br/tesouro/";

    public static AssetValueSnapshot GetSellValue(string bondTitle)
    {
        var slug = DeriveSlug(bondTitle);
        var url = BaseUrl + slug;

        HtmlDocument htmlDoc;
        try
        {
            var htmlWeb = new HtmlWeb();
            htmlDoc = htmlWeb.Load(url);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not load Status Invest page for '{bondTitle}' ({url}).", ex);
        }

        var price = ExtractSellPrice(htmlDoc.DocumentNode.InnerText)
            ?? throw new InvalidOperationException($"Valor Unitario (Venda) not found for '{bondTitle}'. The page structure may have changed.");

        return new AssetValueSnapshot(bondTitle, bondTitle, price, DateTimeOffset.Now);
    }

    internal static string DeriveSlug(string bondTitle)
    {
        var formD = bondTitle.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder();
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stripped.Append(c);
            }
        }

        var cleaned = Regex.Replace(stripped.ToString(), @"[^a-z0-9\s-]", string.Empty);
        var collapsed = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return collapsed.Replace(" ", "-");
    }

    internal static decimal? ExtractSellPrice(string pageText)
    {
        var sellIndex = pageText.IndexOf("venda", StringComparison.OrdinalIgnoreCase);
        if (sellIndex < 0)
        {
            return null;
        }

        var buyIndex = pageText.IndexOf("compra", sellIndex, StringComparison.OrdinalIgnoreCase);
        var searchEnd = buyIndex > sellIndex ? buyIndex : pageText.Length;
        var section = pageText.Substring(sellIndex, searchEnd - sellIndex);

        var labelIndex = section.IndexOf("Valor Unit", StringComparison.OrdinalIgnoreCase);
        if (labelIndex < 0)
        {
            return null;
        }

        var afterLabel = section[labelIndex..];
        var match = Regex.Match(afterLabel, @"R\$\s*([\d.,]+)");
        if (!match.Success)
        {
            return null;
        }

        return ParsePrice(match.Groups[1].Value);
    }

    internal static decimal ParsePrice(string priceText)
    {
        var cleaned = priceText
            .Trim()
            .Replace(".", string.Empty)
            .Replace(",", ".");

        return decimal.Parse(cleaned, CultureInfo.InvariantCulture);
    }
}
