using System.Collections.Concurrent;

namespace Financial.Shared.Abstractions.Currencies;

/// <summary>
/// Decorates the real <see cref="IExchangeRateProvider"/> with a process-lifetime, in-memory cache:
/// a historical FX rate for a past date never changes once published, so once fetched during this
/// run it never needs re-fetching for the rest of it. Not persisted - the cache is empty again on
/// the next app/API start. Only a successful lookup is cached; a null result (transient failure, or
/// a date with genuinely no published rate) is never cached, so it is simply retried next time.
/// </summary>
/// <remarks>
/// Takes a factory rather than a materialized <see cref="IExchangeRateProvider"/> so the real
/// (HttpClient-backed) provider is resolved fresh on every cache miss instead of being captured
/// for this singleton's whole lifetime - a captured typed HttpClient would bypass
/// IHttpClientFactory's handler rotation and risk stale DNS/connections over a long-running process.
/// </remarks>
public sealed class InMemoryCachedExchangeRateProvider : IExchangeRateProvider
{
    private readonly Func<IExchangeRateProvider> _innerFactory;
    private readonly ConcurrentDictionary<string, decimal> _rates = new();

    public InMemoryCachedExchangeRateProvider(Func<IExchangeRateProvider> innerFactory)
    {
        _innerFactory = innerFactory ?? throw new ArgumentNullException(nameof(innerFactory));
    }

    public async Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        var key = BuildKey(date, from, to);
        if (_rates.TryGetValue(key, out var cachedRate))
        {
            return cachedRate;
        }

        var fetchedRate = await _innerFactory().GetHistoricalRateAsync(date, from, to).ConfigureAwait(false);
        if (fetchedRate is decimal rate)
        {
            _rates[key] = rate;
        }

        return fetchedRate;
    }

    private static string BuildKey(DateOnly date, Currency from, Currency to) =>
        $"{date:yyyy-MM-dd}|{from}|{to}";
}
