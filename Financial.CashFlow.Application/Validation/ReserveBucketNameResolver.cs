using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class ReserveBucketNameResolver
{
    public static bool TryResolve(string? name, IEnumerable<ReserveBucket> buckets, out ReserveBucket? bucket)
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
