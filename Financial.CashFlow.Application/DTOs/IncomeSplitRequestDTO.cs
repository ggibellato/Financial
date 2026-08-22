namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to split a single already-net amount across the Reserva buckets.
/// </summary>
public sealed class IncomeSplitRequestDTO
{
    public required DateOnly Date { get; init; }

    public required decimal Amount { get; init; }

    /// <summary>Description to record on each of the 4 posted movements.</summary>
    public required string Description { get; init; }
}
