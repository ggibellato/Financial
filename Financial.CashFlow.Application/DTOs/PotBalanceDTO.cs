namespace Financial.CashFlow.Application.DTOs;

public sealed class PotBalanceDTO
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal SplitPercentage { get; init; }
    public required bool IsActive { get; init; }
    public required decimal Balance { get; init; }
}
