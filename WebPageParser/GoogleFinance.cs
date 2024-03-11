using Financial.Common;
using HtmlAgilityPack;

namespace WebPageParser;

public static class GoogleFinance
{
    public static AssetValue GetFinancialInfo(string exchange, string ticker)
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
            var value = decimal.Parse(nodeString.Replace("R$", "").Replace("£", "").Replace("$", "").Replace("GBX", "").Trim());
            if(nodeString.Contains("GBX"))
            {
                value /= 100;
            }
            var financialData = mainData.ChildNodes[1];
            return new AssetValue(ticker, name, value);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: {ex.Message}");
        }
    }
}

