namespace Financial.Application.DTOs;

public sealed class PortfolioAssetSummaryItemDTO
{
    public string AssetName { get; init; } = string.Empty;
    public string Ticker { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
    public DateTime? FirstInvestmentDate { get; init; }
    public decimal CurrentQuantity { get; init; }
    public decimal TotalBought { get; init; }
    public decimal TotalSold { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal PortfolioWeight { get; init; }
    public decimal TotalCredits { get; init; }
    public IReadOnlyList<AssetCashFlowDTO> CashFlows { get; init; } = [];
}
