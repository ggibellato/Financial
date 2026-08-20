namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// A reserve bucket's current running balance.
/// </summary>
public sealed class ReserveBucketBalanceDTO
{
    public required Guid BucketId { get; init; }
    public required string BucketName { get; init; }
    public required decimal Balance { get; init; }
}
