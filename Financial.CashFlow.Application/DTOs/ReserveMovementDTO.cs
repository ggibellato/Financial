namespace Financial.CashFlow.Application.DTOs;

public sealed class ReserveMovementDTO
{
    public required Guid Id { get; init; }
    public required Guid BucketId { get; init; }
    public required string BucketName { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
}
