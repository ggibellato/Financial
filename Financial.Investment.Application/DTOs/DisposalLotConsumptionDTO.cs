namespace Financial.Investment.Application.DTOs;

public class DisposalLotConsumptionDTO
{
    public Guid? SourceTransactionId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }
}
