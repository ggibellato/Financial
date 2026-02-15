namespace Financial.Application.DTO;

/// <summary>
/// Represents a portfolio node in the navigation tree
/// </summary>
public class PortfolioNodeDTO
{
    /// <summary>
    /// Portfolio name (e.g., "Default", "ISA")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of assets in this portfolio
    /// </summary>
    public int AssetCount { get; set; }

    /// <summary>
    /// Number of active assets (assets with quantity > 0)
    /// </summary>
    public int ActiveAssetCount { get; set; }

    /// <summary>
    /// Assets belonging to this portfolio
    /// </summary>
    public List<AssetNodeDTO> Assets { get; set; } = new();
}
