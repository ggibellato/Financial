namespace Financial.Investment.Application.DTOs;

public class CorporateActionSpinOffUpdateDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string ParentAssetName { get; set; }
    public Guid Id { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal AllocationPercentage { get; set; }
    public string? Note { get; set; }
}
