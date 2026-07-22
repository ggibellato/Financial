using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Services;

public sealed class YearlySummaryService : IYearlySummaryService
{
    private const int MonthsInYear = 12;

    private readonly ICashFlowRepository _repository;

    public YearlySummaryService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<CategoryYearlyTotalDTO> GetCategoryTotalsForYear(int year)
    {
        var expenses = _repository.GetExpenses().Where(e => e.Date.Year == year).ToList();

        return Enum.GetValues<Category>()
            .Select(category =>
            {
                var monthlyTotals = new decimal[MonthsInYear];
                for (var month = 1; month <= MonthsInYear; month++)
                {
                    monthlyTotals[month - 1] = expenses
                        .Where(e => e.Category == category && e.Date.Month == month)
                        .Sum(e => e.Value);
                }

                return new CategoryYearlyTotalDTO
                {
                    Category = category.ToString(),
                    MonthlyTotals = monthlyTotals,
                    YearlyTotal = monthlyTotals.Sum()
                };
            })
            .ToList();
    }

    public InvestmentDiffsYearlyDTO GetInvestmentDiffsForYear(int year)
    {
        var snapshots = _repository.GetInvestmentSnapshots().Where(s => s.Year == year).ToList();

        var accounts = Enum.GetValues<InvestmentAccount>()
            .Select(account =>
            {
                var monthlyValues = new decimal[MonthsInYear];
                for (var month = 1; month <= MonthsInYear; month++)
                {
                    monthlyValues[month - 1] = snapshots
                        .FirstOrDefault(s => s.Account == account && s.Month == month)?.Value ?? 0m;
                }

                return new InvestmentAccountYearlyDiffDTO
                {
                    Account = account.ToString(),
                    IsLiability = InvestmentAccountClassification.IsLiability(account),
                    MonthlyValues = monthlyValues,
                    MonthlyDiffs = ComputeDiffs(monthlyValues)
                };
            })
            .ToList();

        var netPositionValues = new decimal[MonthsInYear];
        for (var month = 0; month < MonthsInYear; month++)
        {
            netPositionValues[month] = accounts
                .Sum(a => a.IsLiability ? -a.MonthlyValues[month] : a.MonthlyValues[month]);
        }

        var netPosition = new NetPositionYearlyDiffDTO
        {
            MonthlyValues = netPositionValues,
            MonthlyDiffs = ComputeDiffs(netPositionValues),
            FullYearNetChange = netPositionValues[MonthsInYear - 1] - netPositionValues[0]
        };

        return new InvestmentDiffsYearlyDTO
        {
            Accounts = accounts.ToArray(),
            NetPosition = netPosition
        };
    }

    private static decimal[] ComputeDiffs(decimal[] monthlyValues)
    {
        var diffs = new decimal[monthlyValues.Length - 1];
        for (var month = 1; month < monthlyValues.Length; month++)
        {
            diffs[month - 1] = monthlyValues[month] - monthlyValues[month - 1];
        }

        return diffs;
    }
}
