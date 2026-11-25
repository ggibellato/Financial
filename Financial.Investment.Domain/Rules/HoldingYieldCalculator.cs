using System;

namespace Financial.Investment.Domain.Rules;

public sealed record HoldingYield(
    decimal? CreditsPercentOfInvested,
    int? MonthsHeld,
    decimal? MonthlyYieldPercent,
    decimal? AprPercent,
    decimal? LastCreditPercentOfMarketValue);

public static class HoldingYieldCalculator
{
    private const int MonthsPerYear = 12;

    public static HoldingYield Calculate(
        decimal invested, decimal totalCredits, DateTime? firstPurchase,
        decimal lastCredit, decimal? marketValue, DateTime today)
    {
        decimal? creditsPercent = invested == 0m ? null : totalCredits / invested * 100m;
        int? monthsHeld = firstPurchase is null ? null : MonthsBetween(firstPurchase.Value, today);
        decimal? monthlyYield = creditsPercent is null || monthsHeld is null ? null : creditsPercent / monthsHeld;
        decimal? lastCreditPercent = marketValue is null or 0m ? null : lastCredit / marketValue * 100m;

        return new HoldingYield(creditsPercent, monthsHeld, monthlyYield, monthlyYield * MonthsPerYear, lastCreditPercent);
    }

    private static int MonthsBetween(DateTime from, DateTime to) =>
        Math.Max(1, (to.Year * MonthsPerYear + to.Month) - (from.Year * MonthsPerYear + from.Month));
}
