namespace Financial.Investment.Application.DTOs;

public class CorporateActionMergerUpdateDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string SourceAssetName { get; set; }
    public Guid Id { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal ExchangeRatio { get; set; }
    public decimal? CashInLieuAmount { get; set; }
    public string? Note { get; set; }
}
