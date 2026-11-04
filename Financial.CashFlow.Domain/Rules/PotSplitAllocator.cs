using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

public static class PotSplitAllocator
{
    public static IReadOnlyList<decimal> Allocate(IReadOnlyList<Pot> pots, decimal baseAmount)
    {
        ArgumentNullException.ThrowIfNull(pots);

        if (pots.Count == 0)
        {
            return [];
        }

        var amounts = pots.Select(pot => pot.CalculateSplitAmount(baseAmount)).ToArray();
        var target = Math.Round(baseAmount * pots.Sum(pot => pot.SplitPercentage) / 100m, 2, MidpointRounding.AwayFromZero);
        var residual = target - amounts.Sum();

        if (residual != 0m)
        {
            amounts[IndexOfLargestPercentage(pots)] += residual;
        }

        return amounts;
    }

    private static int IndexOfLargestPercentage(IReadOnlyList<Pot> pots)
    {
        var largest = 0;
        for (var i = 1; i < pots.Count; i++)
        {
            if (pots[i].SplitPercentage > pots[largest].SplitPercentage)
            {
                largest = i;
            }
        }

        return largest;
    }
}
