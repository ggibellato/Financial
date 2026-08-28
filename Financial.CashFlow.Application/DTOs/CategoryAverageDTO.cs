namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryAverageDTO
{
    public required string Category { get; init; } 
    public required decimal Value { get; init; }
}
