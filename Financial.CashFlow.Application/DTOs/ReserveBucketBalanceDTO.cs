namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// A reserve bucket's current running balance.
/// </summary>
public sealed class ReserveBucketBalanceDTO
{
    public required string Bucket { get; init; }
    public required decimal Balance { get; init; }
}
