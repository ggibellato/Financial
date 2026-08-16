using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class CreditFrequencyAnalyzer
{
    private const double MonthlyMaxAverageGap = 1.5;
    private const double QuarterlyMaxAverageGap = 3.5;
    private const double FourMonthlyMaxAverageGap = 5.0;

    private const int MonthlyPaymentsPerYear = 12;
    private const int QuarterlyPaymentsPerYear = 4;
    private const int FourMonthlyPaymentsPerYear = 3;

    public static int? DetectFrequencyPerYear(IEnumerable<Credit> credits)
    {
        var distinctMonths = credits
            .Select(c => c.Date.Year * 12 + (c.Date.Month - 1))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        if (distinctMonths.Count < 2)
            return null;

        var totalGap = distinctMonths[^1] - distinctMonths[0];
        var averageGap = (double)totalGap / (distinctMonths.Count - 1);

        return averageGap switch
        {
            <= MonthlyMaxAverageGap => MonthlyPaymentsPerYear,
            <= QuarterlyMaxAverageGap => QuarterlyPaymentsPerYear,
            <= FourMonthlyMaxAverageGap => FourMonthlyPaymentsPerYear,
            _ => null
        };
    }
}
