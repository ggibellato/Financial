using Financial.Domain.Entities;
using HtmlAgilityPack;
using System;
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
    private const int MinAssetNameLength = 5;
    private const int MaxAssetNameLength = 100;
    private const int MaxContainerTraversalDepth = 8;

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

    private static HtmlNode GetMainData(HtmlDocument document) =>
        TryGetMainContainerByClass(document)
        ?? TryGetMainContainerByPriceNode(document)
        ?? TryGetMainFallbackTag(document)
        ?? throw new InvalidOperationException("Google Finance main data node not found. The page structure may have changed.");

    internal static HtmlNode? TryGetMainContainerByClass(HtmlDocument document) =>
        document.DocumentNode.SelectSingleNode($"//div[@class='{GoogleFinanceSelectors.MainContainer.PrimaryClass}']");

    internal static HtmlNode? TryGetMainContainerByPriceNode(HtmlDocument document)
    {
        var primaryNode = document.DocumentNode.SelectSingleNode($"//span[@jsname='{GoogleFinanceSelectors.MainContainer.PriceJsName}']");
        if (primaryNode != null)
        {
            var container = TraverseUpToFindContainer(primaryNode);
            if (container != null)
                return container;
        }

        foreach (var alternativeJsName in GoogleFinanceSelectors.MainContainer.AlternativePriceJsNames)
        {
            var priceNode = document.DocumentNode.SelectSingleNode($"//span[@jsname='{alternativeJsName}']");
            if (priceNode != null)
            {
                var container = TraverseUpToFindContainer(priceNode);
                if (container != null)
                    return container;
            }
        }

        return null;
    }

    internal static HtmlNode? TryGetMainFallbackTag(HtmlDocument document) =>
        document.DocumentNode.SelectSingleNode("//main");

    private static HtmlNode? TraverseUpToFindContainer(HtmlNode priceNode)
    {
        // Traverse up to find a suitable container (typically 5-6 levels up)
        var container = priceNode.ParentNode;
        for (int i = 0; i < MaxContainerTraversalDepth && container != null; i++)
        {
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

    private static string ReadAssetName(HtmlNode mainData) =>
        TryReadAssetNameByClass(mainData)
        ?? TryReadAssetNameByContainer(mainData)
        ?? TryReadAssetNameByText(mainData)
        ?? throw new InvalidOperationException("Asset name not found. The page structure may have changed.");

    internal static string? TryReadAssetNameByClass(HtmlNode mainData)
    {
        var nameNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.AssetName.PrimaryClass}']");
        return nameNode?.InnerText.Trim();
    }

    internal static string? TryReadAssetNameByContainer(HtmlNode mainData)
    {
        var containerNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.AssetName.ContainerClass}']");
        var firstDiv = containerNode?.SelectSingleNode(".//div");
        return firstDiv?.InnerText.Trim();
    }

    internal static string? TryReadAssetNameByText(HtmlNode mainData)
    {
        var divNodes = mainData.SelectNodes(".//div");
        if (divNodes == null)
            return null;

        foreach (var div in divNodes.Take(10))
        {
            var text = div.InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(text) && text.Length > MinAssetNameLength && text.Length < MaxAssetNameLength)
                return text;
        }

        return null;
    }

    private static string ReadPriceText(HtmlNode mainData) =>
        TryReadPriceByJsName(mainData)
        ?? TryReadPriceByContainer(mainData)
        ?? TryReadPriceByPattern(mainData)
        ?? throw new InvalidOperationException("Price not found. The page structure may have changed.");

    internal static string? TryReadPriceByJsName(HtmlNode mainData)
    {
        var priceNode = mainData.SelectSingleNode($".//span[@jsname='{GoogleFinanceSelectors.Price.PrimaryJsName}']");
        if (priceNode != null)
            return priceNode.InnerText.Trim();

        foreach (var alternativeJsName in GoogleFinanceSelectors.Price.AlternativeJsNames)
        {
            priceNode = mainData.SelectSingleNode($".//span[@jsname='{alternativeJsName}']");
            if (priceNode != null)
                return priceNode.InnerText.Trim();
        }

        return null;
    }

    internal static string? TryReadPriceByContainer(HtmlNode mainData)
    {
        var priceContainer = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.Price.ContainerClass}']");
        var priceSpan = priceContainer?.SelectSingleNode(".//span");
        return priceSpan?.InnerText.Trim();
    }

    internal static string? TryReadPriceByPattern(HtmlNode mainData)
    {
        var allSpans = mainData.SelectNodes(".//span");
        if (allSpans == null)
            return null;

        foreach (var span in allSpans)
        {
            var text = span.InnerText.Trim();
            if (Regex.IsMatch(text, GoogleFinanceSelectors.Price.PricePattern))
                return text;
        }

        return null;
    }

    private static string ReadAsOfText(HtmlNode mainData) =>
        TryReadAsOfByClass(mainData)
        ?? TryReadAsOfByPattern(mainData)
        ?? string.Empty;

    internal static string? TryReadAsOfByClass(HtmlNode mainData)
    {
        var timeNode = mainData.SelectSingleNode($".//div[@class='{GoogleFinanceSelectors.Timestamp.PrimaryClass}']");
        return timeNode?.InnerText.Trim();
    }

    internal static string? TryReadAsOfByPattern(HtmlNode mainData)
    {
        var allDivs = mainData.SelectNodes(".//div");
        if (allDivs == null)
            return null;

        foreach (var div in allDivs)
        {
            var text = div.InnerText.Trim();
            if (Regex.IsMatch(text, GoogleFinanceSelectors.Timestamp.DatePattern, RegexOptions.IgnoreCase))
                return text;
        }

        return null;
    }
}
