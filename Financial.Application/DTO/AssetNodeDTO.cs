namespace Financial.Application.DTO;

/// <summary>
/// Represents an asset node in the navigation tree
/// </summary>
public class AssetNodeDTO
{
    /// <summary>
    /// Asset name/ticker (e.g., "BCIA11", "VUSA")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Ticker symbol
    /// </summary>
    public required string Ticker { get; set; }

    /// <summary>
    /// Exchange where the asset is traded (e.g., "BVMF", "LSE")
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// ISIN code (if available)
    /// </summary>
    public string ISIN { get; set; } = string.Empty;

    /// <summary>
    /// Current quantity held
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Average purchase price
    /// </summary>
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Whether the asset is active (quantity > 0)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of operations (buy/sell transactions)
    /// </summary>
    public int OperationCount { get; set; }

    /// <summary>
    /// Number of credit entries (dividends/rent)
    /// </summary>
    public int CreditCount { get; set; }
}
