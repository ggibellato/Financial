using Financial.Integrations.WebPageParser;
using Financial.Investment.Domain.ValueObjects;

namespace Financial.Investment.Infrastructure.Services;

internal static class WebPageParserMappers
{
    internal static AssetValueSnapshot ToAssetValueSnapshot(WebAssetQuote quote) =>
        new(quote.Ticker, quote.Name, quote.Price, quote.AsOf);

    internal static List<DividendValue> ToDividendValues(IEnumerable<WebDividendRecord> records) =>
        records.Select(ToDividendValue).ToList();

    private static DividendValue ToDividendValue(WebDividendRecord record) =>
        new(ToDividendType(record.Type), record.Date, record.Value);

    private static DividendType ToDividendType(WebDividendType type) => type switch
    {
        WebDividendType.Dividend => DividendType.Dividend,
        WebDividendType.JCP => DividendType.JCP,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown web dividend type.")
    };
}
