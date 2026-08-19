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
    private readonly ILogger<FallbackFinanceService> _logger;

    public FallbackFinanceService(IFinanceService primary, IFinanceService fallback, ILogger<FallbackFinanceService> logger)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        try
        {
            return _primary.GetAssetValue(request);
        }
        catch (InvalidOperationException primaryException) when (!string.IsNullOrWhiteSpace(request.Exchange))
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
                var snapshot = _fallback.GetAssetValue(request);
                _logger.LogInformation("Fallback finance provider succeeded for ticker {Ticker}", request.Ticker);
                return snapshot;
            }
            catch (Exception fallbackException)
            {
                _logger.LogError(
                    "Both finance providers failed for ticker {Ticker} ({PrimaryErrorType}, {FallbackErrorType})",
                    request.Ticker,
                    primaryException.GetType().Name,
                    fallbackException.GetType().Name);

                throw new InvalidOperationException(
                    $"Both finance providers failed to fetch a price for ticker '{request.Ticker}'.",
                    new AggregateException(primaryException, fallbackException));
            }
        }
    }
}
