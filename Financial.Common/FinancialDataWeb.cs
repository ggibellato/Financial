namespace Financial.Common;

public enum DividendType { Dividend, JCP }

public record AssetValue(string Ticker, string Name, decimal Price);

public record DividendValue(DividendType Type, DateTime Date, decimal Value);
