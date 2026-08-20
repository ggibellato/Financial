using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.Investment.Domain.Rules;

/// <summary>
/// Solves for the extended internal rate of return: the annualised rate at which the net present
/// value of a dated cash-flow series is zero.
/// </summary>
/// <remarks>
/// The rate is solved over the open interval (-1, +infinity). At or below -1 the discount factor
/// Math.Pow(1 + rate, years) raises a negative base to a fractional power, which has no real
/// result, so candidate rates are always kept strictly above -1. The series is bracketed first and
/// then bisected, rather than iterated from a fixed seed, so convergence does not depend on where
/// the search happens to start. A null result therefore means the series admits no rate in that
/// interval — never that the solver gave up on a rate that exists.
/// </remarks>
public static class XirrCalculator
{
    private const double DaysPerYear = 365.0;
    private const double NpvTolerance = 1e-7;
    private const int MaxBisectionIterations = 200;

    // The bracket search probes in (1 + rate) space rather than rate space, so the ladder is dense
    // near the -1 boundary — where near-total-loss rates live — and coarse at high rates.
    private const double MinGrowthFactor = 1e-6;
    private const double MaxGrowthFactor = 1e10;
    private const double GrowthFactorStep = 2.0;

    public static decimal? Calculate(IReadOnlyList<(DateTime Date, decimal Amount)> cashFlows)
    {
        if (cashFlows.Count < 2)
        {
            return null;
        }

        var ordered = cashFlows.OrderBy(cf => cf.Date).ToList();
        var startDate = ordered[0].Date;
        var years = ordered.Select(cf => (cf.Date - startDate).TotalDays / DaysPerYear).ToArray();
        var amounts = ordered.Select(cf => (double)cf.Amount).ToArray();

        if (!SpansMoreThanOneDate(years) || !HasSignChange(amounts))
        {
            return null;
        }

        var bracket = FindBracket(years, amounts);
        if (bracket is null)
        {
            return null;
        }

        var (low, high) = bracket.Value;
        return ToDecimalOrNull(Bisect(years, amounts, low, high));
    }

    /// <summary>
    /// All flows on one date discount by the same factor, so the net present value is a non-zero
    /// constant and no rate can bring it to zero.
    /// </summary>
    private static bool SpansMoreThanOneDate(double[] years) => years[^1] > 0;

    /// <summary>
    /// Without both an inflow and an outflow the net present value never crosses zero, so the
    /// series has no rate at all — a distinct case from one the solver failed to find.
    /// </summary>
    private static bool HasSignChange(double[] amounts) =>
        amounts.Any(amount => amount > 0) && amounts.Any(amount => amount < 0);

    /// <summary>
    /// Walks a ladder of candidate rates looking for the first adjacent pair whose net present
    /// values straddle zero. The ladder only has to bracket the root, not locate it — bisection
    /// does the locating — so a coarse ladder is sufficient for a conventional series.
    /// </summary>
    private static (double Low, double High)? FindBracket(double[] years, double[] amounts)
    {
        double? previousRate = null;
        var previousNpv = 0.0;

        foreach (var rate in ProbeRates())
        {
            var npv = NetPresentValue(years, amounts, rate);
            if (!IsUsable(npv))
            {
                continue;
            }

            if (previousRate is not null && StraddlesZero(previousNpv, npv))
            {
                return (previousRate.Value, rate);
            }

            previousRate = rate;
            previousNpv = npv;
        }

        return null;
    }

    private static IEnumerable<double> ProbeRates()
    {
        for (var growthFactor = MinGrowthFactor;
             growthFactor <= MaxGrowthFactor;
             growthFactor *= GrowthFactorStep)
        {
            yield return growthFactor - 1.0;
        }
    }

    private static bool StraddlesZero(double first, double second) => first * second <= 0;

    private static double Bisect(double[] years, double[] amounts, double low, double high)
    {
        var lowNpv = NetPresentValue(years, amounts, low);

        for (var iteration = 0; iteration < MaxBisectionIterations; iteration++)
        {
            var midpoint = (low + high) / 2.0;
            var midpointNpv = NetPresentValue(years, amounts, midpoint);

            if (Math.Abs(midpointNpv) < NpvTolerance)
            {
                return midpoint;
            }

            if (StraddlesZero(lowNpv, midpointNpv))
            {
                high = midpoint;
            }
            else
            {
                low = midpoint;
                lowNpv = midpointNpv;
            }
        }

        // The interval has been halved 200 times, so it is narrower than double precision can
        // distinguish. The midpoint is the answer even though the absolute tolerance — which is
        // expressed in currency and so does not scale with the size of the series — was not met.
        return (low + high) / 2.0;
    }

    private static double NetPresentValue(double[] years, double[] amounts, double rate)
    {
        var growthFactor = 1.0 + rate;
        if (growthFactor <= 0)
        {
            return double.NaN;
        }

        var presentValue = 0.0;
        for (var index = 0; index < years.Length; index++)
        {
            presentValue += amounts[index] / Math.Pow(growthFactor, years[index]);
        }

        return presentValue;
    }

    private static bool IsUsable(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    /// <summary>
    /// The result is read from WPF data-binding getters, where an OverflowException cannot be
    /// recovered from, so an out-of-range rate is reported as "no rate" instead of thrown.
    /// </summary>
    private static decimal? ToDecimalOrNull(double rate)
    {
        if (double.IsNaN(rate) || rate <= (double)decimal.MinValue || rate >= (double)decimal.MaxValue)
        {
            return null;
        }

        return (decimal)rate;
    }
}
