using System.Diagnostics.CodeAnalysis;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Resolves a spreadsheet bucket column header to a seeded <see cref="ReserveBucket"/> by name,
/// case-insensitively. Name matching lives here rather than in the Application layer because the
/// workbook carries no bucket ids - every other caller identifies a bucket by
/// <see cref="ReserveBucket.Id"/>.
/// </summary>
public static class ReserveBucketNameResolver
{
    public static bool TryResolve(string? name, IEnumerable<ReserveBucket> buckets, [NotNullWhen(true)] out ReserveBucket? bucket)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bucket = null;
            return false;
        }

        bucket = buckets.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
        return bucket is not null;
    }
}
