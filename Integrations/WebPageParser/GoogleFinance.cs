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
        return document.DocumentNode.SelectNodes("//body/c-wiz")[1].SelectNodes("//main")[0];
    }

    private static string ReadAssetName(HtmlNode mainData)
    {
        return mainData.ChildNodes[0].ChildNodes[0].ChildNodes[1].InnerText;
    }

    private static string ReadPriceText(HtmlNode mainData)
    {
        return mainData.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0]
            .ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[0].InnerText;
    }

    private static string ReadAsOfText(HtmlNode mainData)
    {
        return mainData.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0]
            .ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0].InnerText;
    }

}


