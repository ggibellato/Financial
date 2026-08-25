using System;

namespace Financial.CashFlow.Domain.Rules;

public static class AnnualAverageMonthsCalculator
{
    public static int NumberOfMonthsForAverage(DateTimeOffset now, int year) =>
        year switch
        {
            var y when y == now.Year => now.Month - 1,
            2017 => 11,
            _ => 12,
        };
}
