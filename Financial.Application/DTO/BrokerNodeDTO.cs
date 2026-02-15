namespace Financial.Application.DTO;

/// <summary>
/// Represents a broker node in the navigation tree
/// </summary>
public class BrokerNodeDTO
{
    /// <summary>
    /// Broker name (e.g., "XPI", "FreeTrade")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Currency used by this broker (e.g., "BRL", "GBP")
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Number of portfolios under this broker
    /// </summary>
    public int PortfolioCount { get; set; }

    /// <summary>
    /// Total number of assets across all portfolios
    /// </summary>
    public int TotalAssets { get; set; }

    /// <summary>
    /// Portfolios belonging to this broker
    /// </summary>
    public List<PortfolioNodeDTO> Portfolios { get; set; } = new();
}
