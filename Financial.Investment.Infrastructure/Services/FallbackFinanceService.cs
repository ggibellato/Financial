using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

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

    public FallbackFinanceService(IFinanceService primary, IFinanceService fallback)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        try
        {
            return _primary.GetAssetValue(request);
        }
        catch (InvalidOperationException primaryException) when (!string.IsNullOrWhiteSpace(request.Exchange))
        {
            try
            {
                return _fallback.GetAssetValue(request);
            }
            catch (Exception fallbackException)
            {
                throw new InvalidOperationException(
                    $"Both finance providers failed to fetch a price for ticker '{request.Ticker}'.",
                    new AggregateException(primaryException, fallbackException));
            }
        }
    }
}
