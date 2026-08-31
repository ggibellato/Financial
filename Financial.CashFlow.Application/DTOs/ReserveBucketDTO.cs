namespace Financial.CashFlow.Application.DTOs;

public sealed class ReserveBucketDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this bucket currently participates in income splits.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Stored share of a posted income split, 0-100.</summary>
    public required decimal SplitPercentage { get; init; }

    /// <summary>Present only on a Create/Update response, when active buckets' SplitPercentage
    /// doesn't sum to ~100 after this save.</summary>
    public string? Warning { get; init; }
}
