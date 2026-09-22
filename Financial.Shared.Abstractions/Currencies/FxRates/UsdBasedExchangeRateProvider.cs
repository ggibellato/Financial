using Microsoft.Extensions.Logging;

namespace Financial.Shared.Abstractions.Currencies.FxRates;

public sealed class UsdBasedExchangeRateProvider : IExchangeRateProvider
{
    private readonly IFxRateStore _store;
    private readonly IUsdRateFetcher _fetcher;
    private readonly ILogger<UsdBasedExchangeRateProvider> _logger;

    public UsdBasedExchangeRateProvider(
        IFxRateStore store, IUsdRateFetcher fetcher, ILogger<UsdBasedExchangeRateProvider> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        if (from == to)
        {
            return 1m;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        if (date >= today)
        {
            var live = await _fetcher.FetchAsync(date).ConfigureAwait(false);
            return ComputeRate(live.BrlRate, live.GbpRate, from, to, date);
        }

        var stored = _store.TryGetRate(date);
        if (stored is not null)
        {
            return ComputeRate(stored.BrlRate, stored.GbpRate, from, to, date);
        }

        var fetched = await _fetcher.FetchAsync(date).ConfigureAwait(false);
        if (fetched.BrlRate is not null && fetched.GbpRate is not null)
        {
            var record = new FxRateRecord(fetched.BrlRate.Value, fetched.GbpRate.Value, "frankfurter", DateTimeOffset.UtcNow);
            await _store.SetRateAsync(date, record).ConfigureAwait(false);
        }

        return ComputeRate(fetched.BrlRate, fetched.GbpRate, from, to, date);
    }

    private decimal? ComputeRate(decimal? brlRate, decimal? gbpRate, Currency from, Currency to, DateOnly date)
    {
        decimal? Get(Currency currency) => currency switch
        {
            Currency.USD => 1m,
            Currency.BRL => brlRate,
            Currency.GBP => gbpRate,
            _ => null
        };

        var fromRate = Get(from);
        var toRate = Get(to);
        if (fromRate is null || toRate is null)
        {
            return null;
        }

        if (fromRate == 0m || toRate == 0m)
        {
            _logger.LogWarning("Encountered a zero stored FX rate for {Date}; treating as unresolvable.", date);
            return null;
        }

        return toRate / fromRate;
    }
}
