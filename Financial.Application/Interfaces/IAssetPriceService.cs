using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IAssetPriceService
{
    AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request);
}
