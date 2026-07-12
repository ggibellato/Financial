using Financial.Application.DTOs;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class StandardAssetPriceFetcher : IAssetPriceFetcher
{
    public bool Supports(GlobalAssetClass assetClass) => assetClass != GlobalAssetClass.Cryptocurrency;

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Exchange))
        {
            throw new ArgumentException("Exchange is required.", nameof(request));
        }

        return GoogleFinance.GetFinancialInfoSnapshot(request.Exchange, request.Ticker);
    }
}
