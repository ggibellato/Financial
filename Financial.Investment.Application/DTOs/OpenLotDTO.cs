namespace Financial.Investment.Application.DTOs;

public class OpenLotDTO
{
    public Guid SourceTransactionId { get; set; }

    public DateTime Date { get; set; }

    public decimal RemainingQuantity { get; set; }

    public decimal UnitCost { get; set; }
}
