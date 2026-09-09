using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record CreditsAnalysis(
    decimal LastMonthCredits, string? LastCreditMonth, decimal? LastMonthCreditsPercent,
    int? CreditFrequencyPerYear, decimal? EstimatedAnnualCredits, decimal? EstimatedAnnualPercent,
    decimal CurrentMonthCredits);

public static class CreditsAnalysisCalculator
{
    public static CreditsAnalysis Calculate(IReadOnlyCollection<Credit> credits, decimal weightBasis, DateTime today)
    {
        var pastCredits = credits.Where(c => c.Date <= today).ToList();

        var lastCreditGroup = pastCredits
            .GroupBy(c => (c.Date.Year, c.Date.Month))
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .FirstOrDefault();

        var lastMonthCreditsString = lastCreditGroup is not null
            ? $"{lastCreditGroup.Key.Year:D4}-{lastCreditGroup.Key.Month:D2}"
            : null;

        var lastMonthCredits = lastCreditGroup?.Sum(c => c.Value) ?? 0m;

        decimal? lastMonthCreditsPercent = lastCreditGroup is not null && weightBasis != 0m
            ? lastMonthCredits / weightBasis * 100m
            : null;

        var frequencyPerYear = CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits);

        decimal? estimatedAnnualCredits = frequencyPerYear.HasValue
            ? lastMonthCredits * frequencyPerYear.Value
            : null;

        decimal? estimatedAnnualPercent = estimatedAnnualCredits.HasValue && weightBasis != 0m
            ? estimatedAnnualCredits.Value / weightBasis * 100m
            : null;

        var currentMonthCredits = credits
            .Where(c => c.Date.Year == today.Year && c.Date.Month == today.Month)
            .Sum(c => c.Value);

        return new CreditsAnalysis(
            lastMonthCredits, lastMonthCreditsString, lastMonthCreditsPercent,
            frequencyPerYear, estimatedAnnualCredits, estimatedAnnualPercent,
            currentMonthCredits);
    }
}
