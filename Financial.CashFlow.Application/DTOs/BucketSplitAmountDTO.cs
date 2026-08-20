namespace Financial.CashFlow.Application.DTOs;

/// <summary>One bucket's computed share of a posted income split.</summary>
public sealed class BucketSplitAmountDTO
{
    public required Guid BucketId { get; init; }
    public required string BucketName { get; init; }
    public required decimal Amount { get; init; }
}
