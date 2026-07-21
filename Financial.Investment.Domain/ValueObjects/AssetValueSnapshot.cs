using System;

namespace Financial.Investment.Domain.ValueObjects;

public record AssetValueSnapshot(string Ticker, string Name, decimal Price, DateTimeOffset AsOf);
