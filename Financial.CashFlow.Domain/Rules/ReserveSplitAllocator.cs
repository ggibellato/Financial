using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

public static class ReserveSplitAllocator
{
    public static IReadOnlyList<decimal> Allocate(IReadOnlyList<ReserveBucket> buckets, decimal baseAmount)
    {
        ArgumentNullException.ThrowIfNull(buckets);

        if (buckets.Count == 0)
        {
            return [];
        }

        var amounts = buckets.Select(bucket => bucket.CalculateSplitAmount(baseAmount)).ToArray();
        var target = Math.Round(baseAmount * buckets.Sum(b => b.SplitPercentage) / 100m, 2, MidpointRounding.AwayFromZero);
        var residual = target - amounts.Sum();

        if (residual != 0m)
        {
            amounts[IndexOfLargestPercentage(buckets)] += residual;
        }

        return amounts;
    }

    private static int IndexOfLargestPercentage(IReadOnlyList<ReserveBucket> buckets)
    {
        var largest = 0;
        for (var i = 1; i < buckets.Count; i++)
        {
            if (buckets[i].SplitPercentage > buckets[largest].SplitPercentage)
            {
                largest = i;
            }
        }

        return largest;
    }
}
