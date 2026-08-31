namespace Financial.CashFlow.Application.DTOs;

public sealed class ReserveBucketCreateDTO
{
    public required string Name { get; init; }

    public required decimal SplitPercentage { get; init; }

    public required bool IsActive { get; init; }
}
