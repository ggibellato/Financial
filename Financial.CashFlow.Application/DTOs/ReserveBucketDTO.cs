namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked reserve bucket.
/// </summary>
public sealed class ReserveBucketDTO
{
    /// <summary>Reserve bucket identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Reserve bucket display name.</summary>
    public required string Name { get; init; }

    /// <summary>Whether this bucket currently participates in income splits.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Stored share of a posted income split, 0-100.</summary>
    public required decimal SplitPercentage { get; init; }
}
