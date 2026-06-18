using Financial.Domain.Entities;
using HtmlAgilityPack;
using System.Globalization;

namespace Financial.Infrastructure.Integrations.WebPageParser;

public sealed class DadosMercadoDividend
{
    private const int DividendTypeColumn = 0;
    private const int DividendValueColumn = 1;
    private const int DividendDateColumn = 4;
    private const int MinimumColumnCount = 5;
    private const string DividendTypeCode = "Dividendo";

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
        if (cells.Count < MinimumColumnCount)
        {
            throw new InvalidOperationException("Dividend row does not contain expected columns.");
        }

        var dividendType = cells[DividendTypeColumn].InnerText == DividendTypeCode ? DividendType.Dividend : DividendType.JCP;
        var date = DateTime.Parse(cells[DividendDateColumn].InnerText);
        var value = decimal.Parse(
            cells[DividendValueColumn].InnerText.Replace(",", ".").Replace("* ", ""),
            CultureInfo.InvariantCulture);

        return new DividendValue(dividendType, date, value);
    }
}

