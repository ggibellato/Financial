using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;

namespace Financial.Infrastructure.Interfaces;

public interface IAssetPriceFetcher
{
    bool Supports(GlobalAssetClass assetClass);

    AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request);
}
