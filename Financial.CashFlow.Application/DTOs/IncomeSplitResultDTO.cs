namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// The amounts posted to each active Reserva bucket by an income split, plus their total for
/// immediate display — no need to re-sum the movement history to know how much was split.
/// </summary>
public sealed class IncomeSplitResultDTO
{
    public required IReadOnlyList<BucketSplitAmountDTO> Buckets { get; init; }
    public required decimal Total { get; init; }
}
