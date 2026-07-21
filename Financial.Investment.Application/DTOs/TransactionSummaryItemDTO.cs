namespace Financial.Investment.Application.DTOs;

/// <summary>
/// Represents a single transaction within a broker- or portfolio-scoped combined transaction list
/// </summary>
public class TransactionSummaryItemDTO
{
    /// <summary>
    /// Name of the asset the transaction belongs to
    /// </summary>
    public required string AssetName { get; set; }

    /// <summary>
    /// Date of the transaction
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Type of transaction (Buy or Sell)
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Total price (UnitPrice * Quantity + Fees)
    /// </summary>
    public decimal TotalPrice { get; set; }
}
