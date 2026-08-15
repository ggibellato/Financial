using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IPriceService
{
    Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request);
    Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request);
}
