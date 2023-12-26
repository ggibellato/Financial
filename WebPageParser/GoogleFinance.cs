using Financial.Common;
using HtmlAgilityPack;

namespace WebPageParser;

public static class GoogleFinance
{
    public static AssetValue GetFinancialInfo(string ticker)
    {
        var googleTickerSearch = $"https://www.google.com/finance/quote/{ticker}:BVMF";
        //var content = await HTMLHelper.LoadPage(googleTickerSearch);
        try
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc = htmlWeb.Load(googleTickerSearch);

            var mainData = htmlDoc.DocumentNode.SelectNodes("//body/c-wiz")[1].SelectNodes("//main")[0];
            var name = mainData.ChildNodes[0].ChildNodes[0].ChildNodes[1].InnerText;
            var value = decimal.Parse(mainData.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0]
                .ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes[0].InnerText.Replace("R$", ""));
            var financialData = mainData.ChildNodes[1];
            return new AssetValue(ticker, name, value);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: {ex.Message}");
        }
    }
}

