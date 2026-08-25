namespace Financial.Integrations.WebPageParser;

public enum WebDividendType { Dividend, JCP }

public sealed record WebDividendRecord(WebDividendType Type, DateTime Date, decimal Value);
