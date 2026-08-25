using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IAssetPriceLookupService
{
    /// <summary>
    /// Returns the asset's current price, preferring a live fetch. A successful live fetch is
    /// recorded into the asset's Price History as an automatic entry (skipping the write when
    /// today's entry is already an unchanged automatic one). When the live fetch fails and the
    /// request identifies the asset (<see cref="AssetPriceRequestDTO.PortfolioName"/> and
    /// <see cref="AssetPriceRequestDTO.AssetName"/>), falls back to today's Price History entry
    /// instead of throwing. Rethrows the original exception when no such entry exists.
    /// </summary>
    Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request);
}
