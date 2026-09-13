using System;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Domain.Entities;

/// <summary>
/// A fixed audit record of the exchange rate that applied when a Transaction or Credit was
/// recorded - captured once at entry time and never recomputed afterward, even if the reporting
/// currency setting later changes.
/// </summary>
public class FxRateSnapshot
{
    public Currency ToCurrency { get; private set; }

    public decimal Rate { get; private set; }

    public FxRateSource Source { get; private set; }

    public DateTimeOffset RetrievedAt { get; private set; }

    private FxRateSnapshot() { }

    public static FxRateSnapshot Create(Currency toCurrency, decimal rate, FxRateSource source, DateTimeOffset retrievedAt)
    {
        if (rate <= 0)
        {
            throw new ArgumentException("Rate must be greater than zero.", nameof(rate));
        }

        return new()
        {
            ToCurrency = toCurrency,
            Rate = rate,
            Source = source,
            RetrievedAt = retrievedAt
        };
    }
}
