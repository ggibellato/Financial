namespace Financial.CashFlow.Application.DTOs;

public sealed class TitheMonthDTO
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required TitheSummaryDTO Summary { get; init; }
}
