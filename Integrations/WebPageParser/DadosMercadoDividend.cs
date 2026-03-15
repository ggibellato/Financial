using Financial.Common;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Financial.Infrastructure.Integrations.WebPageParser;

public class DadosMercadoDividend
{
    public static List<DividendValue> GetDividendInfo(string ticker)
    {
        var googleTickerSearch = $"https://www.dadosdemercado.com.br/bolsa/acoes/{ticker}/dividendos";
        var result = new List<DividendValue>();
        HtmlWeb htmlWeb = new HtmlWeb();
        HtmlDocument htmlDoc = htmlWeb.Load(googleTickerSearch);
        var table = htmlDoc.DocumentNode.SelectSingleNode("//table[contains(@class, 'normal-table')]")
            ?? throw new InvalidOperationException("Dividend table not found.");
        var rows = table.SelectNodes("tbody/tr")
            ?? throw new InvalidOperationException("Dividend rows not found.");

        foreach (HtmlNode row in rows)
        {
            var data = row.SelectNodes("th|td")
                ?? throw new InvalidOperationException("Dividend row has no cells.");
            result.Add(ParseDividendRow(data.ToList()));
        }

        return result;
    }

    internal static DividendValue ParseDividendRow(IReadOnlyList<HtmlNode> cells)
    {
        if (cells.Count < 5)
        {
            throw new InvalidOperationException("Dividend row does not contain expected columns.");
        }

        var dividendType = cells[0].InnerText == "Dividendo" ? DividendType.Dividend : DividendType.JCP;
        var date = DateTime.Parse(cells[4].InnerText);
        var value = decimal.Parse(
            cells[1].InnerText.Replace(",", ".").Replace("* ", ""),
            CultureInfo.InvariantCulture);

        return new DividendValue(dividendType, date, value);
    }
}

