namespace Financial.CashFlow.Application.DTOs;

public sealed class TitheCarryForwardDTO
{
    /// <summary>Snapshotted amount available to carry in, fixed at creation time.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Whether this amount currently counts toward the month's Tithe Balance.</summary>
    public required bool Included { get; init; }

    public required int FromYear { get; init; }

    public required int FromMonth { get; init; }
}
