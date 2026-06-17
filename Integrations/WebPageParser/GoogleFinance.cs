using Financial.Domain.Entities;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Financial.Infrastructure.Integrations.WebPageParser;

/// <summary>
/// Parses financial data from Google Finance web pages.
/// Uses multi-strategy approach with fallbacks to handle HTML structure changes.
/// See GoogleFinance.Selectors.md for maintenance guide.
/// Update selectors in GoogleFinanceSelectors.cs when the HTML structure changes.
/// </summary>
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
        HtmlWeb htmlWeb = new HtmlWeb();
        HtmlDocument htmlDoc = htmlWeb.Load(googleTickerSearch);

        var mainData = GetMainData(htmlDoc);
        var name = ReadAssetName(mainData);
        var priceText = ReadPriceText(mainData);
        var value = GoogleFinanceParsing.ParsePriceValue(priceText);

        var asOfText = ReadAsOfText(mainData);
        var asOf = GoogleFinanceParsing.TryParseAsOf(asOfText) ?? DateTimeOffset.Now;
        return new AssetValueSnapshot(ticker, name, value, asOf);
    }

    private static HtmlNode GetMainData(HtmlDocument document)
    {
        // Strategy 1: Look for the main container which contains all financial data
        var mainDataNode = document.DocumentNode.SelectSingleNode($"//div[@class='{GoogleFinanceSelectors.MainContainer.PrimaryClass}']");
        if (mainDataNode != null)
        {
            return mainDataNode;
        }

        // Strategy 2a: Look for the price element with primary jsname and traverse up
        var priceNode = document.DocumentNode.SelectSingleNode($"//span[@jsname='{GoogleFinanceSelectors.MainContainer.PriceJsName}']");
        if (priceNode != null)
        {
            var container = TraverseUpToFindContainer(priceNode);
            if (container != null)
                return container;
        }

        // Strategy 2b: Try alternative jsname values for price element
        foreach (var alternativeJsName in GoogleFinanceSelectors.MainContainer.AlternativePriceJsNames)
        {
            priceNode = document.DocumentNode.SelectSingleNode($"//span[@jsname='{alternativeJsName}']");
            if (priceNode != null)
            {
                var container = TraverseUpToFindContainer(priceNode);
                if (container != null)
                    return container;
            }
        }

        // Strategy 3: Fallback to main tag (less specific but more stable)
        var mainTag = document.DocumentNode.SelectSingleNode("//main");
        if (mainTag != null)
        {
            return mainTag;
        }

        throw new InvalidOperationException("Google Finance main data node not found. The page structure may have changed.");
    }

    private static HtmlNode? TraverseUpToFindContainer(HtmlNode priceNode)
    {
        // Traverse up to find a suitable container (typically 5-6 levels up)
        var container = priceNode.ParentNode;
        for (int i = 0; i < 8 && container != null; i++)
        {
            // Look for a container that also has the asset name
            var xpath = $".//div[contains(@class, '{GoogleFinanceSelectors.AssetName.PrimaryClass}')]";
            var nameNode = container.SelectSingleNode(xpath);
            if (nameNode != null)
            {
                return container;
            }
            container = container.ParentNode;
        }
        return null;
    }

    private static string ReadAssetName(HtmlNode mainData)
    {
        // Strategy 1: Look for class which contains the asset name
        var nameNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.AssetName.PrimaryClass}']");
        if (nameNode != null)
        {
            return nameNode.InnerText.Trim();
        }

        // Strategy 2: Look for container and get first child div
        var containerNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.AssetName.ContainerClass}']");
        if (containerNode != null)
        {
            var firstDiv = containerNode.SelectSingleNode(".//div");
            if (firstDiv != null)
            {
                return firstDiv.InnerText.Trim();
            }
        }

        // Strategy 3: Fallback - look for first substantial text in early divs
        var divNodes = mainData.SelectNodes(".//div");
        if (divNodes != null)
        {
            foreach (var div in divNodes.Take(10))
            {
                var text = div.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 5 && text.Length < 100)
                {
                    return text;
                }
            }
        }

        throw new InvalidOperationException("Asset name not found. The page structure may have changed.");
    }

    private static string ReadPriceText(HtmlNode mainData)
    {
        // Strategy 1a: Look for the price by primary jsname attribute (most stable)
        var priceNode = mainData.SelectSingleNode($".//span[@jsname='{GoogleFinanceSelectors.Price.PrimaryJsName}']");
        if (priceNode != null)
        {
            return priceNode.InnerText.Trim();
        }

        // Strategy 1b: Try alternative jsname values
        foreach (var alternativeJsName in GoogleFinanceSelectors.Price.AlternativeJsNames)
        {
            priceNode = mainData.SelectSingleNode($".//span[@jsname='{alternativeJsName}']");
            if (priceNode != null)
            {
                return priceNode.InnerText.Trim();
            }
        }

        // Strategy 2: Look for price container class
        var priceContainer = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.Price.ContainerClass}']");
        if (priceContainer != null)
        {
            var priceSpan = priceContainer.SelectSingleNode(".//span");
            if (priceSpan != null)
            {
                return priceSpan.InnerText.Trim();
            }
        }

        // Strategy 3: Look for common price patterns in the HTML
        var allSpans = mainData.SelectNodes(".//span");
        if (allSpans != null)
        {
            foreach (var span in allSpans)
            {
                var text = span.InnerText.Trim();
                // Match patterns like: $123.45, R$19.17, £45.67, 123.45 GBX
                if (Regex.IsMatch(text, GoogleFinanceSelectors.Price.PricePattern))
                {
                    return text;
                }
            }
        }

        throw new InvalidOperationException("Price not found. The page structure may have changed.");
    }

    private static string ReadAsOfText(HtmlNode mainData)
    {
        // Strategy 1: Look for timestamp container class
        var timeNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.Timestamp.PrimaryClass}']");
        if (timeNode != null)
        {
            return timeNode.InnerText.Trim();
        }

        // Strategy 2: Look for text patterns that contain date/time information
        var allDivs = mainData.SelectNodes(".//div");
        if (allDivs != null)
        {
            foreach (var div in allDivs)
            {
                var text = div.InnerText.Trim();
                // Look for patterns like "Jun 5, 10:44:17 PM UTC-3" or similar
                if (Regex.IsMatch(text, GoogleFinanceSelectors.Timestamp.DatePattern, RegexOptions.IgnoreCase))
                {
                    return text;
                }
            }
        }

        // Strategy 3: Return empty string to trigger fallback to DateTimeOffset.Now in caller
        return string.Empty;
    }

}


