using Financial.Application.DTOs;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;

namespace Financial.Infrastructure.Interfaces;

public interface IAssetPriceFetcher
{
    bool Supports(GlobalAssetClass assetClass);

    AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request);
}
