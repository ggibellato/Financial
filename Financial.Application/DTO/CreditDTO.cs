namespace Financial.Application.DTO;

/// <summary>
/// Represents a credit entry (dividend or rent) for an asset
/// </summary>
public class CreditDTO
{
    /// <summary>
    /// Date the credit was received
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Type of credit (Dividend or Rent)
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Credit amount
    /// </summary>
    public decimal Value { get; set; }
}
