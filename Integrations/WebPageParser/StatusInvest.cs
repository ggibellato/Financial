using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HtmlAgilityPack;

namespace Financial.Integrations.WebPageParser;

/// <summary>
/// Parses Status Invest's per-bond page for the "Valor de Venda" (sell price).
/// The real page markup could not be inspected for CSS classes/IDs while this
/// parser was written, so it extracts by text pattern rather than selectors.
/// Run StatusInvestVerificationTests manually to confirm it still matches production.
/// </summary>
public static class StatusInvest
{
    private const string BaseUrl = "https://statusinvest.com.br/tesouro/";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0 Safari/537.36";
    private const int MaxLoadAttempts = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static WebAssetQuote GetSellValue(string bondTitle)
    {
        var slug = DeriveSlug(bondTitle);
        var url = BaseUrl + slug;

        var htmlDoc = LoadWithRetry(bondTitle, url);

        var price = ExtractSellPrice(htmlDoc.DocumentNode.InnerText)
            ?? throw new InvalidOperationException($"Valor Unitario (Venda) not found for '{bondTitle}'. The page structure may have changed.");

        return new WebAssetQuote(bondTitle, bondTitle, price, DateTimeOffset.Now);
    }

    // Status Invest sits behind a bot-challenge (Cloudflare) that occasionally answers a normal
    // request with a 403 "Just a moment..." page instead of the bond page. HtmlWeb does not throw
    // for that - it hands back the challenge HTML - so without this check ExtractSellPrice finds
    // nothing and the caller wrongly concludes the page structure changed. One retry on the same
    // HtmlWeb (so any challenge cookie it received carries over) is enough to clear a transient block.
    private static HtmlDocument LoadWithRetry(string bondTitle, string url)
    {
        var htmlWeb = new HtmlWeb { UserAgent = UserAgent, UseCookies = true };
        InvalidOperationException? lastBlockedResponse = null;

        for (var attempt = 1; attempt <= MaxLoadAttempts; attempt++)
        {
            HtmlDocument htmlDoc;
            try
            {
                htmlDoc = htmlWeb.Load(url);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not load Status Invest page for '{bondTitle}' ({url}).", ex);
            }

            if (htmlWeb.StatusCode == HttpStatusCode.OK)
            {
                return htmlDoc;
            }

            lastBlockedResponse = new InvalidOperationException(
                $"Status Invest returned {(int)htmlWeb.StatusCode} for '{bondTitle}' ({url}); the request was likely blocked rather than the page structure changing.");

            if (attempt < MaxLoadAttempts)
            {
                Thread.Sleep(RetryDelay);
            }
        }

        throw lastBlockedResponse!;
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
