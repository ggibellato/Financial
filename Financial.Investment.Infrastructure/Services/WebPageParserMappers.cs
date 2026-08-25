using Financial.Integrations.WebPageParser;
using Financial.Investment.Domain.ValueObjects;

namespace Financial.Investment.Infrastructure.Services;

internal static class WebPageParserMappers
{
    internal static AssetValueSnapshot ToAssetValueSnapshot(WebAssetQuote quote) =>
        new(quote.Ticker, quote.Name, quote.Price, quote.AsOf);
}
