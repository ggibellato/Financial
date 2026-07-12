using System.Globalization;
using Financial.Domain.ValueObjects;
using HtmlAgilityPack;

namespace Financial.Infrastructure.Integrations.WebPageParser;

/// <summary>
/// Parses the Tesouro Direto redemption-price table.
/// Column positions are resolved by header text (not fixed indices) since the
/// live table structure could not be verified when this scraper was written.
/// Run TesouroDiretoVerificationTests manually to confirm it still matches production.
/// </summary>
public static class TesouroDireto
{
    private const string RedemptionPageUrl = "https://www.tesourodireto.com.br/produtos/dados-sobre-titulos/rendimento-dos-titulos";
    private const string TitleColumnHeader = "Título";
    private const string PriceColumnHeader = "Preço Unit.";
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public static AssetValueSnapshot? GetRedemptionValue(string bondTitle)
    {
        var htmlWeb = new HtmlWeb { UserAgent = BrowserUserAgent };
        HtmlDocument htmlDoc = htmlWeb.Load(RedemptionPageUrl);

        var table = FindRedemptionTable(htmlDoc)
            ?? throw new InvalidOperationException("Tesouro Direto redemption table not found. The page structure may have changed.");

        return FindMatchingRow(table, bondTitle);
    }

    internal static HtmlNode? FindRedemptionTable(HtmlDocument document)
    {
        var tables = document.DocumentNode.SelectNodes("//table");
        if (tables == null)
        {
            return null;
        }

        foreach (var table in tables)
        {
            if (TryResolveColumns(table, out _, out _))
            {
                return table;
            }
        }

        return null;
    }

    internal static AssetValueSnapshot? FindMatchingRow(HtmlNode table, string bondTitle)
    {
        if (!TryResolveColumns(table, out var titleColumnIndex, out var priceColumnIndex))
        {
            throw new InvalidOperationException("Tesouro Direto table columns not found. The page structure may have changed.");
        }

        var rows = table.SelectNodes(".//tbody/tr");
        if (rows == null)
        {
            return null;
        }

        var requestedTitle = bondTitle.Trim();

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("th|td");
            var lastColumnIndex = Math.Max(titleColumnIndex, priceColumnIndex);
            if (cells == null || cells.Count <= lastColumnIndex)
            {
                continue;
            }

            var rowTitle = cells[titleColumnIndex].InnerText.Trim();
            if (!string.Equals(rowTitle, requestedTitle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var price = ParsePrice(cells[priceColumnIndex].InnerText);
            return new AssetValueSnapshot(bondTitle, rowTitle, price, DateTimeOffset.Now);
        }

        return null;
    }

    internal static bool TryResolveColumns(HtmlNode table, out int titleColumnIndex, out int priceColumnIndex)
    {
        titleColumnIndex = -1;
        priceColumnIndex = -1;

        var headerRow = table.SelectSingleNode(".//thead/tr") ?? table.SelectSingleNode(".//tr");
        var headerCells = headerRow?.SelectNodes("th|td");
        if (headerCells == null)
        {
            return false;
        }

        for (var i = 0; i < headerCells.Count; i++)
        {
            var headerText = headerCells[i].InnerText.Trim();
            if (headerText.Contains(TitleColumnHeader, StringComparison.OrdinalIgnoreCase))
            {
                titleColumnIndex = i;
            }
            else if (headerText.Contains(PriceColumnHeader, StringComparison.OrdinalIgnoreCase))
            {
                priceColumnIndex = i;
            }
        }

        return titleColumnIndex >= 0 && priceColumnIndex >= 0;
    }

    internal static decimal ParsePrice(string priceText)
    {
        var cleaned = priceText
            .Replace("R$", string.Empty)
            .Trim()
            .Replace(".", string.Empty)
            .Replace(",", ".");

        return decimal.Parse(cleaned, CultureInfo.InvariantCulture);
    }
}
