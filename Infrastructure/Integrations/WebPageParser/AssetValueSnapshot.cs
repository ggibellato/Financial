namespace Financial.Infrastructure.Integrations.WebPageParser;

public sealed record AssetValueSnapshot(string Ticker, string Name, decimal Price, DateTimeOffset AsOf);
