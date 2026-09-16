using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Services;

/// <summary>Caches one rate lookup per distinct date for a single currency pair. The cache is keyed
/// on date alone, so converting from a second source currency needs a second instance.</summary>
internal sealed class CurrencyConversionContext(Currency from, Currency to, IExchangeRateProvider exchangeRateProvider)
{
    private readonly Dictionary<DateOnly, decimal?> _rateCache = [];

    public int AttemptCount { get; private set; }
    public int FailureCount { get; private set; }
    public bool IsUnavailable => AttemptCount > 0 && FailureCount == AttemptCount;
    public bool IsPartial => FailureCount > 0;

    public async Task<decimal?> ConvertAsync(decimal amount, DateOnly date)
    {
        if (from == to)
        {
            return amount;
        }

        if (!_rateCache.TryGetValue(date, out var rate))
        {
            AttemptCount++;
            rate = await exchangeRateProvider.GetHistoricalRateAsync(date, from, to).ConfigureAwait(false);
            _rateCache[date] = rate;
            if (rate is null)
            {
                FailureCount++;
            }
        }

        return rate.HasValue ? amount * rate.Value : null;
    }
}
