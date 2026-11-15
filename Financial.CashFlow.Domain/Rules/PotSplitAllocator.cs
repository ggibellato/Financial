using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

public static class PotSplitAllocator
{
    private const decimal Cent = 0.01m;

    public static IReadOnlyList<decimal> Allocate(IReadOnlyList<Pot> pots, decimal baseAmount)
    {
        ArgumentNullException.ThrowIfNull(pots);

        if (pots.Count == 0)
        {
            return [];
        }

        var amounts = pots.Select(pot => pot.CalculateSplitAmount(baseAmount)).ToArray();
        var target = MoneyRounding.ToCents(baseAmount * pots.Sum(pot => pot.SplitPercentage) / 100m);
        var residual = target - amounts.Sum();

        if (residual != 0m)
        {
            SpreadResidual(amounts, pots, baseAmount, residual);
        }

        return amounts;
    }

    private static void SpreadResidual(decimal[] amounts, IReadOnlyList<Pot> pots, decimal baseAmount, decimal residual)
    {
        var byLargestShare = Enumerable.Range(0, pots.Count).OrderByDescending(index => pots[index].SplitPercentage).ToList();
        var largest = byLargestShare[0];
        if (KeepsTheBaseSign(amounts[largest] + residual, baseAmount))
        {
            amounts[largest] += residual;
            return;
        }

        var step = Math.Sign(residual) * Cent;
        foreach (var index in byLargestShare.Take((int)(residual / step)))
        {
            amounts[index] += step;
        }
    }

    private static bool KeepsTheBaseSign(decimal share, decimal baseAmount) => Math.Sign(share) * Math.Sign(baseAmount) >= 0;
}
