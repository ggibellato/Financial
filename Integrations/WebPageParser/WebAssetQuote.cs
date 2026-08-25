namespace Financial.Integrations.WebPageParser;

public sealed record WebAssetQuote(string Ticker, string Name, decimal Price, DateTimeOffset AsOf);
