namespace Financial.Investment.Application.DTOs;

public class SetAssetPriceDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public DateOnly Date { get; set; }
    public decimal Price { get; set; }
}

public class DeleteAssetPriceDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public DateOnly Date { get; set; }
}
