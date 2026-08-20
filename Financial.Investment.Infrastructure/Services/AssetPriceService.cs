using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class AssetPriceService : IAssetPriceService
{
    private readonly IEnumerable<IAssetPriceFetcher> _fetchers;

    public AssetPriceService(IEnumerable<IAssetPriceFetcher> fetchers)
    {
        _fetchers = fetchers ?? throw new ArgumentNullException(nameof(fetchers));
    }

    public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker is required.", nameof(request));
        }

        if (!_fetchers.Any())
        {
            throw new InvalidOperationException("No asset price fetcher is registered.");
        }

        // Falling back to the first registered fetcher used to hide an unsupported class behind
        // a lookup that was always going to fail - a private-credit holding was asked for as an
        // equity ticker. An unsupported class is now named as such.
        var fetcher = _fetchers.FirstOrDefault(f => f.Supports(request.AssetClass))
            ?? throw new NotSupportedException($"No price source supports the asset class '{request.AssetClass}'.");

        var snapshot = fetcher.GetSnapshot(request);

        return new AssetPriceDTO
        {
            Exchange = request.Exchange,
            Ticker = snapshot.Ticker,
            Name = snapshot.Name,
            Price = snapshot.Price,
            AsOf = snapshot.AsOf
        };
    }
}
