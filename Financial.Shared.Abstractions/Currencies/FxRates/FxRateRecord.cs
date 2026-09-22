namespace Financial.Shared.Abstractions.Currencies.FxRates;

public sealed record FxRateRecord(
    decimal BrlRate,
    decimal GbpRate,
    string Source,
    DateTimeOffset StoredAt);
