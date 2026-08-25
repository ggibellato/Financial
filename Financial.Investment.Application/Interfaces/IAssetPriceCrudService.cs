using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IAssetPriceCrudService
{
    Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request);
    Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request);
}
