using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public sealed class PortfolioAssetSummaryItemDTO
{
    public string AssetName { get; init; } = string.Empty;
    public string Ticker { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass Class { get; init; } = GlobalAssetClass.Unknown;
    public DateTime? FirstInvestmentDate { get; init; }
    public decimal CurrentQuantity { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal? AverageSellPrice { get; init; }
    public decimal TotalBought { get; init; }
    public decimal TotalSold { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal RealizedGainLoss { get; init; }
    public decimal PortfolioWeight { get; init; }
    public decimal TotalCredits { get; init; }
    public IReadOnlyList<AssetCashFlowDTO> CashFlows { get; init; } = [];
    public decimal LastMonthCredits { get; init; }
    public string? LastCreditMonth { get; init; }
    public decimal? LastMonthCreditsPercent { get; init; }
    public int? CreditFrequencyPerYear { get; init; }
    public decimal? EstimatedAnnualCredits { get; init; }
    public decimal? EstimatedAnnualPercent { get; init; }
    public decimal CurrentMonthCredits { get; init; }
}
