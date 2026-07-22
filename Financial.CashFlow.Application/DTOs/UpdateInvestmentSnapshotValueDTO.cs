namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update a single investment snapshot's value.
/// </summary>
public sealed class UpdateInvestmentSnapshotValueDTO
{
    public required decimal Value { get; init; }
}
