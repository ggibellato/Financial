using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Infrastructure.DTOs;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class BondAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly IFinanceService _statusInvestFinanceService;

    public BondAssetPriceFetcher(IFinanceService statusInvestFinanceService)
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

        return _statusInvestFinanceService.GetAssetValue(new AssetValueRequestDTO { Name = request.Name });
    }
}
