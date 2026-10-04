using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public sealed class DeterministicExchangeRateProvider : IExchangeRateProvider
{
    private static readonly IReadOnlyDictionary<Currency, decimal> UnitsPerUsd = new Dictionary<Currency, decimal>
    {
        [Currency.USD] = 1m,
        [Currency.GBP] = 0.8m,
        [Currency.BRL] = 5m
    };

    public int CallCount { get; private set; }

    public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        CallCount++;
        return Task.FromResult<decimal?>(from == to ? 1m : UnitsPerUsd[to] / UnitsPerUsd[from]);
    }
}
