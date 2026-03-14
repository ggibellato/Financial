using System;
using System.Linq;
using Financial.Common;
using HtmlAgilityPack;

namespace Financial.Infrastructure.Integrations.WebPageParser;

public static class GoogleFinance
{
    public static AssetValue GetFinancialInfo(string exchange, string ticker)
    {
        var snapshot = GetFinancialInfoSnapshot(exchange, ticker);
        return new AssetValue(snapshot.Ticker, snapshot.Name, snapshot.Price);
    }

    public static AssetValueSnapshot GetFinancialInfoSnapshot(string exchange, string ticker)
    {
        var googleTickerSearch = $"https://www.google.com/finance/quote/{ticker}:{exchange}";
        //var content = await HTMLHelper.LoadPage(googleTickerSearch);
        try
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc = htmlWeb.Load(googleTickerSearch);

            var mainData = htmlDoc.DocumentNode.SelectNodes("//body/c-wiz")[1].SelectNodes("//main")[0];
            var name = mainData.ChildNodes[0].ChildNodes[0].ChildNodes[1].InnerText;
            var nodeString = mainData.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0]
                .ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[0].InnerText;
            var value = decimal.Parse(nodeString.Replace("R$", "").Replace("?", "").Replace("$", "").Replace("GBX", "").Replace("£", "").Trim());
            if (nodeString.Contains("GBX"))
            {
                value /= 100;
            }

            var asOf = TryParseAsOf(htmlDoc) ?? DateTimeOffset.Now;
            return new AssetValueSnapshot(ticker, name, value, asOf);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: {ex.Message}", ex);
        }
    }

    private static DateTimeOffset? TryParseAsOf(HtmlDocument htmlDoc)
    {
        var lastUpdated = htmlDoc.DocumentNode.Descendants()
            .Select(node => node.GetAttributeValue("data-last-updated", null))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (long.TryParse(lastUpdated, out var lastUpdatedMs))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(lastUpdatedMs);
        }

        var asOfText = htmlDoc.DocumentNode.Descendants()
            .Select(node => node.InnerText?.Trim())
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text) &&
                                    text.Contains("As of", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(asOfText))
        {
            var index = asOfText.IndexOf("As of", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var candidate = asOfText[(index + "As of".Length)..].Trim().TrimStart(':').Trim();
                if (DateTimeOffset.TryParse(candidate, out var asOf))
                {
                    return asOf;
                }
            }
        }

        return null;
    }
}


