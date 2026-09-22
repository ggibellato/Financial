namespace Financial.CashFlow.Application.DTOs;

public sealed class BucketSplitAmountDTO
{
    public required Guid BucketId { get; init; }
    public required string BucketName { get; init; }
    public required decimal Amount { get; init; }
}
