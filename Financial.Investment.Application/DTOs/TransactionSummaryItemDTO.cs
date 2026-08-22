namespace Financial.Investment.Application.DTOs;

public class TransactionSummaryItemDTO
{
    public required string AssetName { get; set; }

    public DateTime Date { get; set; }

    public required string Type { get; set; }

    public decimal TotalPrice { get; set; }
}
