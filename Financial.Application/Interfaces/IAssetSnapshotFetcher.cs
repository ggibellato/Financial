using Financial.Application.DTOs;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;

namespace Financial.Application.Interfaces;

public interface IAssetSnapshotFetcher
{
    bool Supports(GlobalAssetClass assetClass);

    AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request);
}
