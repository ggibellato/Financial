using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;

namespace Financial.Investment.Infrastructure.Interfaces;

public interface IAssetPriceFetcher
{
    bool Supports(GlobalAssetClass assetClass, ValuationMethod valuationMethod);

    AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request);
}
