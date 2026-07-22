namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a single reserve ledger movement.
/// </summary>
public sealed class ReserveMovementDTO
{
    public required Guid Id { get; init; }
    public required string Bucket { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
}
