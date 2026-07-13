using Financial.Application.DTOs;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class BondAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly StatusInvestFinanceService _statusInvestFinanceService;

    public BondAssetPriceFetcher(StatusInvestFinanceService statusInvestFinanceService)
    {
        _statusInvestFinanceService = statusInvestFinanceService ?? throw new ArgumentNullException(nameof(statusInvestFinanceService));
    }

    public bool Supports(GlobalAssetClass assetClass) => assetClass == GlobalAssetClass.Bond;

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required for bond assets.", nameof(request));
        }

        return _statusInvestFinanceService.GetAssetValue(new AssetValueRequest { Name = request.Name });
    }
}
