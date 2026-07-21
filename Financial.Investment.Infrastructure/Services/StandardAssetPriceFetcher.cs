using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class StandardAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly IFinanceService _financeService;

    public StandardAssetPriceFetcher(IFinanceService financeService)
    {
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
    }

    public bool Supports(GlobalAssetClass assetClass) => assetClass != GlobalAssetClass.Cryptocurrency && assetClass != GlobalAssetClass.Bond;

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Exchange))
        {
            throw new ArgumentException("Exchange is required.", nameof(request));
        }

        return _financeService.GetAssetValue(new AssetValueRequestDTO { Exchange = request.Exchange, Ticker = request.Ticker });
    }
}
