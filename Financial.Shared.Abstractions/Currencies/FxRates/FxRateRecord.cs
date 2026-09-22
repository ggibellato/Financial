namespace Financial.Shared.Abstractions.Currencies.FxRates;

public sealed record FxRateRecord(
    string Base,
    decimal BrlRate,
    decimal GbpRate,
    string Source,
    DateTimeOffset StoredAt);
