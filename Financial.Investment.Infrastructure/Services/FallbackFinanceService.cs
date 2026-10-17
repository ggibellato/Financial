using System.Collections.Concurrent;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Infrastructure.Services;

/// <summary>
/// Tries <paramref name="primary"/> first; on failure for an exchange-based (stock) lookup,
/// retries with <paramref name="fallback"/>. Crypto lookups (identified by a missing Exchange)
/// are not retried, since the fallback provider doesn't support the Currency-based lookup shape.
/// </summary>
public sealed class FallbackFinanceService : IFinanceService
{
    private readonly IFinanceService _primary;
    private readonly IFinanceService _fallback;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _failureCooldown;
    private readonly ILogger<FallbackFinanceService> _logger;
    private readonly ConcurrentDictionary<ProviderAttempt, DateTimeOffset> _lastFailures = new();

    public FallbackFinanceService(
        IFinanceService primary,
        IFinanceService fallback,
        TimeProvider timeProvider,
        TimeSpan failureCooldown,
        ILogger<FallbackFinanceService> logger)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _failureCooldown = failureCooldown;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Exchange))
        {
            return _primary.GetAssetValue(request);
        }

        return IsCoolingDown(request, _primary) && !IsCoolingDown(request, _fallback)
            ? FallbackFirst(request)
            : PrimaryFirst(request);
    }

    private AssetValueSnapshot PrimaryFirst(AssetValueRequestDTO request)
    {
        try
        {
            return Attempt(_primary, request);
        }
        catch (InvalidOperationException primaryException)
        {
            // Degraded-but-working behavior must be visible in the log stream
            // (logging-audit.md priority 3). Tickers are public market symbols, not
            // personal financial data, so naming one here is within FR-014.
            _logger.LogWarning(
                "Primary finance provider failed for ticker {Ticker} with {ErrorType}; trying fallback provider",
                request.Ticker,
                primaryException.GetType().Name);

            try
            {
                return AttemptFallback(request);
            }
            catch (Exception fallbackException)
            {
                throw BothProvidersFailed(request, primaryException, fallbackException);
            }
        }
    }

    private AssetValueSnapshot FallbackFirst(AssetValueRequestDTO request)
    {
        try
        {
            return AttemptFallback(request);
        }
        catch (Exception fallbackException)
        {
            _logger.LogWarning(
                "Fallback finance provider failed for ticker {Ticker} with {ErrorType}; retrying primary provider",
                request.Ticker,
                fallbackException.GetType().Name);

            try
            {
                return Attempt(_primary, request);
            }
            catch (InvalidOperationException primaryException)
            {
                throw BothProvidersFailed(request, primaryException, fallbackException);
            }
        }
    }

    private AssetValueSnapshot AttemptFallback(AssetValueRequestDTO request)
    {
        var snapshot = Attempt(_fallback, request);
        _logger.LogInformation("Fallback finance provider succeeded for ticker {Ticker}", request.Ticker);
        return snapshot;
    }

    private AssetValueSnapshot Attempt(IFinanceService provider, AssetValueRequestDTO request)
    {
        var attempt = ProviderAttempt.For(request, provider);
        try
        {
            var snapshot = provider.GetAssetValue(request);
            _lastFailures.TryRemove(attempt, out _);
            return snapshot;
        }
        catch (Exception ex) when (IsProviderFailure(provider, ex))
        {
            _lastFailures[attempt] = _timeProvider.GetUtcNow();
            throw;
        }
    }

    private bool IsProviderFailure(IFinanceService provider, Exception ex) =>
        provider != _primary || ex is InvalidOperationException;

    private bool IsCoolingDown(AssetValueRequestDTO request, IFinanceService provider) =>
        _lastFailures.TryGetValue(ProviderAttempt.For(request, provider), out var failedAt)
        && _timeProvider.GetUtcNow() - failedAt < _failureCooldown;

    private InvalidOperationException BothProvidersFailed(
        AssetValueRequestDTO request,
        InvalidOperationException primaryException,
        Exception fallbackException)
    {
        _logger.LogError(
            "Both finance providers failed for ticker {Ticker} ({PrimaryErrorType}, {FallbackErrorType})",
            request.Ticker,
            primaryException.GetType().Name,
            fallbackException.GetType().Name);

        return new InvalidOperationException(
            $"Both finance providers failed to fetch a price for ticker '{request.Ticker}'.",
            new AggregateException(primaryException, fallbackException));
    }

    private readonly record struct ProviderAttempt(string? Exchange, string? Ticker, string? Name, IFinanceService Provider)
    {
        public static ProviderAttempt For(AssetValueRequestDTO request, IFinanceService provider) =>
            new(request.Exchange, request.Ticker, request.Name, provider);
    }
}
