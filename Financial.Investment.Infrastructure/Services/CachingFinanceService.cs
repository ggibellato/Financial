using System.Collections.Concurrent;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class CachingFinanceService : IFinanceService
{
    private readonly IFinanceService _inner;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _cacheDuration;
    private readonly ConcurrentDictionary<QuoteKey, CachedQuote> _quotes = new();

    public CachingFinanceService(IFinanceService inner, TimeProvider timeProvider, TimeSpan cacheDuration)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _cacheDuration = cacheDuration;
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = new QuoteKey(request.Ticker, request.Exchange, request.Currency, request.Name);
        if (_quotes.TryGetValue(key, out var cached) && _timeProvider.GetUtcNow() - cached.FetchedAt < _cacheDuration)
        {
            return cached.Snapshot;
        }

        var snapshot = _inner.GetAssetValue(request);
        _quotes[key] = new CachedQuote(snapshot, _timeProvider.GetUtcNow());
        return snapshot;
    }

    private readonly record struct QuoteKey(string? Ticker, string? Exchange, string? Currency, string? Name);

    private readonly record struct CachedQuote(AssetValueSnapshot Snapshot, DateTimeOffset FetchedAt);
}
