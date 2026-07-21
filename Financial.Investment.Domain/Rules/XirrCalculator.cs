using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.Investment.Domain.Rules;

public static class XirrCalculator
{
    private const int MaxIterations = 100;
    private const double Tolerance = 1e-7;
    private const double InitialRateGuess = 0.1;
    private const double DaysPerYear = 365.0;

    public static decimal? Calculate(IReadOnlyList<(DateTime Date, decimal Amount)> cashFlows)
    {
        if (cashFlows.Count < 2)
        {
            return null;
        }

        var ordered = cashFlows.OrderBy(cf => cf.Date).ToList();
        var startDate = ordered[0].Date;
        var rate = InitialRateGuess;

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            double presentValue = 0;
            double derivative = 0;

            foreach (var (date, amount) in ordered)
            {
                var years = (date - startDate).TotalDays / DaysPerYear;
                var discountFactor = Math.Pow(1 + rate, years);
                var amountAsDouble = (double)amount;

                presentValue += amountAsDouble / discountFactor;
                derivative -= years * amountAsDouble / (discountFactor * (1 + rate));
            }

            if (Math.Abs(presentValue) < Tolerance)
            {
                return (decimal)rate;
            }

            if (derivative == 0)
            {
                return null;
            }

            rate -= presentValue / derivative;
        }

        return null;
    }
}
