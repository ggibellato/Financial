using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class BondAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly IFinanceService _financeService;

    public BondAssetPriceFetcher(IFinanceService financeService)
    {
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
    }

    public bool Supports(GlobalAssetClass assetClass, ValuationMethod valuationMethod) =>
        valuationMethod == ValuationMethod.BondQuote
        || (valuationMethod == ValuationMethod.Unspecified && assetClass == GlobalAssetClass.Bond);

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required for bond assets.", nameof(request));
        }

        return _financeService.GetAssetValue(new AssetValueRequestDTO
        {
            Name = request.Name,
            Exchange = request.Exchange,
            Ticker = request.Ticker
        });
    }
}
