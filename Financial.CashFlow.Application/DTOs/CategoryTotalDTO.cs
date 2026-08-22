namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryTotalDTO
{
    public required string Category { get; init; }

    public required decimal TotalValue { get; init; }
}
