namespace Financial.Application.DTO;

/// <summary>
/// Represents a buy or sell operation for an asset
/// </summary>
public class OperationDTO
{
    /// <summary>
    /// Date of the operation
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Type of operation (Buy or Sell)
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Quantity traded
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Transaction fees
    /// </summary>
    public decimal Fees { get; set; }

    /// <summary>
    /// Total price (UnitPrice * Quantity + Fees)
    /// </summary>
    public decimal TotalPrice { get; set; }
}
