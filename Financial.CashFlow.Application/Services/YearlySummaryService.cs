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
        var totalsByCategoryAndMonth = _repository.GetExpenses()
            .Where(e => e.Date.Year == year)
            .GroupBy(e => (e.Category, e.Date.Month))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Value));

        return Enum.GetValues<Category>()
            .Select(category =>
            {
                var monthlyTotals = new decimal[MonthsInYear];
                for (var month = 1; month <= MonthsInYear; month++)
                {
                    monthlyTotals[month - 1] = totalsByCategoryAndMonth.GetValueOrDefault((category, month));
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
        var valueByAccountAndMonth = _repository.GetInvestmentSnapshots()
            .Where(s => s.Year == year)
            .GroupBy(s => (s.Account, s.Month))
            .ToDictionary(g => g.Key, g => g.First().Value);

        var accounts = Enum.GetValues<InvestmentAccount>()
            .Select(account =>
            {
                var monthlyValues = new decimal[MonthsInYear];
                for (var month = 1; month <= MonthsInYear; month++)
                {
                    monthlyValues[month - 1] = valueByAccountAndMonth.GetValueOrDefault((account, month));
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
