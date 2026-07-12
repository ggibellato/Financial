using System;

namespace Financial.Domain.ValueObjects;

public enum DividendType { Dividend, JCP }

public record AssetValueSnapshot(string Ticker, string Name, decimal Price, DateTimeOffset AsOf);

public record DividendValue(DividendType Type, DateTime Date, decimal Value);
