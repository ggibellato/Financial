using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

/// <summary>
/// The per-bucket fan-out shared by the manual "New Income Split" flow and the automated
/// Income-triggered split - one <see cref="ReserveMovement"/> per active bucket, using each
/// bucket's own percentage rule. No validation: callers apply their own request-level rules first.
/// </summary>
internal static class ReserveSplitMovementFactory
{
    public static List<ReserveMovement> Create(
        IEnumerable<ReserveBucket> activeBuckets, decimal amount, DateOnly date, string description, Income? income = null) =>
        activeBuckets
            .Select(bucket => ReserveMovement.Create(bucket, bucket.CalculateSplitAmount(amount), date, description, income))
            .ToList();
}
