using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Financial.Integrations.WebPageParser;

/// <summary>
/// "PU Venda Manha" is the unit price a holder gets selling - what Status Invest calls "Valor de Venda". The file
/// keeps matured series, so a held bond that is no longer offered still resolves to its last published price.
/// </summary>
public static class TesouroTransparente
{
    private const string PriceListUrl =
        "https://www.tesourotransparente.gov.br/ckan/dataset/df56aa42-484a-4a59-8184-7676580c81e3/resource/796d2059-14e9-44e3-80c9-2d9e30b405c1/download/PrecoTaxaTesouroDireto.csv";

    private const string TypeColumn = "Tipo Titulo";
    private const string MaturityColumn = "Data Vencimento";
    private const string BaseDateColumn = "Data Base";
    private const string SellPriceColumn = "PU Venda Manha";
    private const string DateFormat = "dd/MM/yyyy";

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(2) };
    private static readonly object CacheGate = new();
    private static IReadOnlyList<PriceRow>? _cachedRows;
    private static long _cachedAtTimestamp;

    public static WebAssetQuote GetSellValue(string bondTitle)
    {
        var price = FindSellValue(LoadRows(), bondTitle);
        return new WebAssetQuote(bondTitle, bondTitle, price, DateTimeOffset.Now);
    }

    internal static IReadOnlyList<PriceRow> ParseLatestPrices(TextReader csv)
    {
        var header = csv.ReadLine()?.Split(';')
            ?? throw new InvalidOperationException("The Tesouro Transparente price list is empty.");

        var typeIndex = ColumnIndex(header, TypeColumn);
        var maturityIndex = ColumnIndex(header, MaturityColumn);
        var baseDateIndex = ColumnIndex(header, BaseDateColumn);
        var priceIndex = ColumnIndex(header, SellPriceColumn);
        var lastIndex = new[] { typeIndex, maturityIndex, baseDateIndex, priceIndex }.Max();

        var latest = new Dictionary<(string Type, DateOnly Maturity), PriceRow>();
        while (csv.ReadLine() is { } line)
        {
            var cells = line.Split(';');
            if (cells.Length <= lastIndex || !TryParseRow(cells, typeIndex, maturityIndex, baseDateIndex, priceIndex, out var row))
            {
                continue;
            }

            var key = (row.Type, row.Maturity);
            if (!latest.TryGetValue(key, out var current) || row.BaseDate > current.BaseDate)
            {
                latest[key] = row;
            }
        }

        return latest.Values.ToList();
    }

    internal static decimal FindSellValue(IReadOnlyList<PriceRow> rows, string bondTitle)
    {
        var (type, year) = SplitTitle(bondTitle);

        var matches = rows
            .Where(row => row.Type == type && row.Maturity.Year == year)
            .ToList();

        return matches.Count switch
        {
            0 => throw new InvalidOperationException(
                $"'{bondTitle}' is not in the Tesouro Transparente price list."),
            1 => matches[0].SellPrice,
            _ => throw new InvalidOperationException(
                $"'{bondTitle}' matches more than one maturity in the Tesouro Transparente price list."),
        };
    }

    internal static string NormalizeType(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return Regex.Replace(withoutMarks, @"\s+", " ");
    }

    private static (string Type, int Year) SplitTitle(string bondTitle)
    {
        var match = Regex.Match(NormalizeType(bondTitle), @"^(?<type>.+?)\s+(?<year>\d{4})$");
        if (!match.Success)
        {
            throw new InvalidOperationException($"'{bondTitle}' is not a bond title ending in a maturity year.");
        }

        return (match.Groups["type"].Value, int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture));
    }

    private static bool TryParseRow(string[] cells, int typeIndex, int maturityIndex, int baseDateIndex, int priceIndex, out PriceRow row)
    {
        row = default!;
        if (!DateOnly.TryParseExact(cells[maturityIndex], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var maturity)
            || !DateOnly.TryParseExact(cells[baseDateIndex], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var baseDate)
            || !decimal.TryParse(cells[priceIndex].Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return false;
        }

        row = new PriceRow(NormalizeType(cells[typeIndex]), maturity, baseDate, price);
        return true;
    }

    private static int ColumnIndex(string[] header, string column)
    {
        var index = Array.FindIndex(header, name => string.Equals(name.Trim(), column, StringComparison.OrdinalIgnoreCase));
        return index >= 0
            ? index
            : throw new InvalidOperationException($"The Tesouro Transparente price list has no '{column}' column. The file layout may have changed.");
    }

    private static IReadOnlyList<PriceRow> LoadRows()
    {
        lock (CacheGate)
        {
            if (_cachedRows is not null && Stopwatch.GetElapsedTime(_cachedAtTimestamp) < CacheLifetime)
            {
                return _cachedRows;
            }

            try
            {
                using var stream = Client.GetStreamAsync(PriceListUrl).GetAwaiter().GetResult();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                _cachedRows = ParseLatestPrices(reader);
                _cachedAtTimestamp = Stopwatch.GetTimestamp();
                return _cachedRows;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                throw new InvalidOperationException("Could not load the Tesouro Transparente price list.", ex);
            }
        }
    }

    internal sealed record PriceRow(string Type, DateOnly Maturity, DateOnly BaseDate, decimal SellPrice);
}
