using Financial.Domain.Entities;

namespace Financial.Application.DTOs;

public class AssetPriceRequestDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
    public GlobalAssetClass AssetClass { get; set; } = GlobalAssetClass.Unknown;
    public string? BrokerName { get; set; }
    public string? Name { get; set; }
}
