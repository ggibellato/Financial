using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IAssetPriceService
{
    AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request);
}
