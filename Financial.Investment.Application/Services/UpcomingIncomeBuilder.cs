using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal static class UpcomingIncomeBuilder
{
    private const int MonthsPerYear = 12;

    internal static UpcomingIncomeDTO? TryBuildEntry(Asset asset, string brokerName)
    {
        var paymentsPerYear = CreditFrequencyAnalyzer.DetectFrequencyPerYear(asset.Credits);
        if (paymentsPerYear is null)
        {
            return null;
        }

        var lastCredit = asset.Credits.MaxBy(credit => credit.Date)!;

        return new UpcomingIncomeDTO(
            asset.Name,
            brokerName,
            lastCredit.Date,
            lastCredit.Date.AddMonths(MonthsPerYear / paymentsPerYear.Value),
            lastCredit.NetAmount);
    }
}
