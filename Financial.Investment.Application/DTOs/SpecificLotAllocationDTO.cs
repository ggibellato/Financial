namespace Financial.Investment.Application.DTOs;

public class SpecificLotAllocationDTO
{
    public required Guid SourceTransactionId { get; set; }
    public decimal Quantity { get; set; }
}
