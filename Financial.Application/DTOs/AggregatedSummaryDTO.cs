namespace Financial.Application.DTOs;

public sealed class AggregatedSummaryDTO
{
    public decimal TotalBought { get; init; }
    public decimal TotalSold { get; init; }
    public decimal TotalCredits { get; init; }
}
