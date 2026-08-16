using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class AssetPriceRequestDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
    public GlobalAssetClass AssetClass { get; set; } = GlobalAssetClass.Unknown;
    public string? BrokerName { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// The asset's portfolio, used together with <see cref="AssetName"/> to look up its
    /// Price History for the current-value fallback. Optional — when either is missing,
    /// the fallback is skipped and behavior is identical to a plain live-price lookup.
    /// </summary>
    public string? PortfolioName { get; set; }

    /// <summary>
    /// The asset's domain name (composite key with <see cref="BrokerName"/>/<see cref="PortfolioName"/>),
    /// used for the Price History fallback. See <see cref="PortfolioName"/>.
    /// </summary>
    public string? AssetName { get; set; }
}
