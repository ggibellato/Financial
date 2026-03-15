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
        try
        {
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
        catch (Exception ex)
        {
            throw new Exception($"Error: {ex.Message}", ex);
        }
    }

    private static HtmlNode GetMainData(HtmlDocument document)
    {
        var rootNodes = document.DocumentNode.SelectNodes("//body/c-wiz");
        if (rootNodes == null || rootNodes.Count <= 1)
        {
            throw new InvalidOperationException("Google Finance main data node not found.");
        }

        var mainNodes = rootNodes[1].SelectNodes("//main");
        if (mainNodes == null || mainNodes.Count == 0)
        {
            throw new InvalidOperationException("Google Finance main content not found.");
        }

        return mainNodes[0];
    }

    private static string ReadAssetName(HtmlNode mainData)
    {
        return ReadNodeText(mainData, "Asset name", 0, 0, 1);
    }

    private static string ReadPriceText(HtmlNode mainData)
    {
        return ReadNodeText(mainData, "Price text", 1, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    private static string ReadAsOfText(HtmlNode mainData)
    {
        return ReadNodeText(mainData, "As-of text", 1, 0, 0, 0, 0, 0, 0, 1, 0);
    }

    private static string ReadNodeText(HtmlNode root, string description, params int[] childPath)
    {
        return GetNodeByPath(root, description, childPath).InnerText;
    }

    private static HtmlNode GetNodeByPath(HtmlNode root, string description, params int[] childPath)
    {
        var current = root;
        foreach (var index in childPath)
        {
            if (current.ChildNodes.Count <= index)
            {
                throw new InvalidOperationException($"{description} node not found at index {index}.");
            }

            current = current.ChildNodes[index];
        }

        return current;
    }

}


