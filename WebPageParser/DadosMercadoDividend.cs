using Financial.Common;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebPageParser;

public class DadosMercadoDividend
{
    public static List<DividendValue> GetDividendInfo(string ticker)
    {
        var googleTickerSearch = $"https://www.dadosdemercado.com.br/bolsa/acoes/{ticker}/dividendos";
        //var content = await HTMLHelper.LoadPage(googleTickerSearch);
        try
        {
            var result = new List<DividendValue>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc = htmlWeb.Load(googleTickerSearch);
            var table = htmlDoc.DocumentNode.SelectSingleNode("//table[contains(@class, 'normal-table')]");
            foreach (HtmlNode row in table.SelectNodes("tbody/tr"))
            {
                var data = row.SelectNodes("th|td");
                var dividend = new DividendValue(
                    data[0].InnerText == "Dividendo" ? DividendType.Dividend : DividendType.JCP,
                    DateTime.Parse(data[4].InnerText),
                    decimal.Parse(data[1].InnerText.Replace(",", "."))
                );
                result.Add(dividend);
            }
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: {ex.Message}");
        }
    }
}
