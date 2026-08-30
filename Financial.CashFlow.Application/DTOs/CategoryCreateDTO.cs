namespace Financial.CashFlow.Application.DTOs;

public sealed class CategoryCreateDTO
{
    public required string Name { get; init; }

    public required bool Active { get; init; }

    public required bool IsInvestment { get; init; }

    public required bool IsTithe { get; init; }
}
