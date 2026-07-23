namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to split a single already-net amount across the Reserva buckets.
/// </summary>
public sealed class IncomeSplitRequestDTO
{
    /// <summary>Date to post the resulting movements under.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>The amount to split across the Reserva buckets.</summary>
    public required decimal Amount { get; init; }
}
