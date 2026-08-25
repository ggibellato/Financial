using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IAssetPriceHistoryService
{
    Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request);
    Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request);
}
