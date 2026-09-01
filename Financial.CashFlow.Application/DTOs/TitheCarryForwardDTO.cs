namespace Financial.CashFlow.Application.DTOs;

public sealed class TitheCarryForwardDTO
{
    /// <summary>Snapshotted amount available to carry in, fixed at creation time.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Whether this amount currently counts toward the month's Tithe Balance.</summary>
    public required bool Included { get; init; }

    /// <summary>Year of the month this amount was carried from.</summary>
    public required int FromYear { get; init; }

    /// <summary>Month (1-12) this amount was carried from.</summary>
    public required int FromMonth { get; init; }
}
