namespace Financial.CashFlow.Application.DTOs;

public sealed class PotUpdateDTO
{
    public Guid? Id { get; init; }
    public required string Name { get; init; }
    public required decimal SplitPercentage { get; init; }
    public required bool IsActive { get; init; }
}
