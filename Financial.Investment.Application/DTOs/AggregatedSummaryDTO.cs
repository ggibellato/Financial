namespace Financial.Investment.Application.DTOs;

public sealed class AggregatedSummaryDTO
{
    public decimal TotalBought { get; init; }
    public decimal TotalSold { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal? MarketValue { get; init; }
    public int HoldingCount { get; init; }
    public int UnvaluedHoldingCount { get; init; }
    public decimal? PriceOnlyReturn { get; init; }
    public decimal? TotalReturn { get; init; }
    public decimal? TotalReturnNetOfTax { get; init; }
}
