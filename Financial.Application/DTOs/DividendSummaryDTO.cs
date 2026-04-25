namespace Financial.Application.DTOs;

public class DividendSummaryDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
    public required string Name { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTimeOffset PriceAsOf { get; set; }
    public decimal AverageDividendLastFiveYears { get; set; }
    public decimal PriceMaxBuy { get; set; }
    public decimal DiscountPercent { get; set; }
    public required IReadOnlyList<DividendYearTotalDTO> YearTotals { get; set; }
}
