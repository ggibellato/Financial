namespace Financial.Investment.Application.DTOs;

public class CorporateActionSplitUpdateDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public Guid Id { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal RatioFactor { get; set; }
    public string? Note { get; set; }
}
