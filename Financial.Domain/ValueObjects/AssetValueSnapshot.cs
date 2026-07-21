using System;

namespace Financial.Domain.ValueObjects;

public record AssetValueSnapshot(string Ticker, string Name, decimal Price, DateTimeOffset AsOf);
