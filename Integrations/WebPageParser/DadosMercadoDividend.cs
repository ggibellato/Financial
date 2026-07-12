using Financial.Domain.ValueObjects;
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
        var table = htmlDoc.DocumentNode.SelectSingleNode("//table[contains(@class, 'normal-table')]");
        if (table == null) return result;

        var rows = table.SelectNodes("tbody/tr");
        if (rows == null) return result;

        foreach (HtmlNode row in rows)
        {
            var data = row.SelectNodes("th|td");
            if (data == null || data.Count < MinimumColumnCount) continue;
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
        var date = DateTime.ParseExact(cells[DividendDateColumn].InnerText.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
        var value = decimal.Parse(
            cells[DividendValueColumn].InnerText.Replace(",", ".").Replace("* ", ""),
            CultureInfo.InvariantCulture);

        return new DividendValue(dividendType, date, value);
    }
}

