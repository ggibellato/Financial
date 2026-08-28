namespace Financial.CashFlow.Application.DTOs;

public sealed class ReserveMovementUpdateDTO
{
    public required Guid BucketId { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
}
