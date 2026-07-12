using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

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

        var fetcher = _fetchers.FirstOrDefault(f => f.Supports(request.AssetClass))
            ?? _fetchers.FirstOrDefault();

        if (fetcher is null)
        {
            throw new InvalidOperationException("No asset price fetcher is registered.");
        }

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
