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
        var allSnapshots = _repository.GetInvestmentSnapshots().ToList();
        var allAccounts = _repository.GetInvestmentAccounts().ToList();
        var scopedAccounts = YearScopedInvestmentAccountResolver.ResolveForYear(allAccounts, allSnapshots, year, DateTime.Now.Year);

        var valueByAccountAndMonth = allSnapshots
            .Where(s => s.Year == year)
            .GroupBy(s => (s.Account, s.Month))
            .ToDictionary(g => g.Key, g => g.First().Value);

        var priorYearDecemberByAccount = allSnapshots
            .Where(s => s.Year == year - 1 && s.Month == MonthsInYear)
            .GroupBy(s => s.Account, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

        var hasPriorYearData = allSnapshots.Any(s => s.Year == year - 1);

        var accounts = scopedAccounts
            .Select(account =>
            {
                var monthlyValues = new decimal[MonthsInYear];
                for (var month = 1; month <= MonthsInYear; month++)
                {
                    monthlyValues[month - 1] = valueByAccountAndMonth.GetValueOrDefault((account.Name, month));
                }

                decimal? januaryDiff = hasPriorYearData
                    ? monthlyValues[0] - priorYearDecemberByAccount.GetValueOrDefault(account.Name, 0m)
                    : null;

                return new InvestmentAccountYearlyDiffDTO
                {
                    Account = account.Name,
                    IsLiability = account.IsLiability,
                    MonthlyValues = monthlyValues,
                    MonthlyDiffs = ComputeDiffs(monthlyValues, januaryDiff)
                };
            })
            .ToList();

        var netPositionValues = new decimal[MonthsInYear];
        for (var month = 0; month < MonthsInYear; month++)
        {
            netPositionValues[month] = accounts
                .Sum(a => a.IsLiability ? -a.MonthlyValues[month] : a.MonthlyValues[month]);
        }

        decimal? netPositionJanuaryDiff = hasPriorYearData
            ? accounts.Sum(a => (a.IsLiability ? -1 : 1) * a.MonthlyDiffs[0]!.Value)
            : null;

        var netPositionDiffs = ComputeDiffs(netPositionValues, netPositionJanuaryDiff);

        // A future month (beyond the current calendar month, for the current year) has no
        // snapshot yet, so its value defaults to 0 and would misrepresent a real drop to zero.
        // FullYearNetChange, Average, and Sum all stop at the year's last relevant month:
        // December for a past year, or the current calendar month for the current year.
        var lastRelevantMonth = year >= DateTime.Now.Year ? Math.Min(DateTime.Now.Month, MonthsInYear) : MonthsInYear;
        var relevantDiffs = netPositionDiffs.Take(lastRelevantMonth).Where(d => d.HasValue).Select(d => d!.Value).ToList();

        var netPosition = new NetPositionYearlyDiffDTO
        {
            MonthlyValues = netPositionValues,
            MonthlyDiffs = netPositionDiffs,
            FullYearNetChange = netPositionValues[lastRelevantMonth - 1] - netPositionValues[0],
            AverageMonthResult = relevantDiffs.Count > 0 ? relevantDiffs.Average() : 0m,
            SumOfMonthResults = relevantDiffs.Sum()
        };

        return new InvestmentDiffsYearlyDTO
        {
            Accounts = accounts.ToArray(),
            NetPosition = netPosition
        };
    }

    public IncomeYearlySummaryDTO GetIncomeSummaryForYear(int year)
    {
        var salaryMonthly = new decimal[MonthsInYear];
        var salaryAfterTaxesMonthly = new decimal[MonthsInYear];
        var dividendoJurosMonthly = new decimal[MonthsInYear];

        foreach (var income in _repository.GetIncomes().Where(i => i.Date.Year == year))
        {
            var monthIndex = income.Date.Month - 1;

            if (income.IncomeSource is IncomeSource.Gleison or IncomeSource.Ariana)
            {
                salaryMonthly[monthIndex] += income.GrossValue ?? 0m;
                salaryAfterTaxesMonthly[monthIndex] += income.NetValue;
            }
            else if (income.IncomeSource == IncomeSource.DividendoJuros)
            {
                dividendoJurosMonthly[monthIndex] += income.NetValue;
            }
        }

        var taxDifferenceMonthly = new decimal[MonthsInYear];
        for (var month = 0; month < MonthsInYear; month++)
        {
            taxDifferenceMonthly[month] = salaryMonthly[month] - salaryAfterTaxesMonthly[month];
        }

        return new IncomeYearlySummaryDTO
        {
            SalaryMonthly = salaryMonthly,
            SalaryYearlyTotal = salaryMonthly.Sum(),
            SalaryAfterTaxesMonthly = salaryAfterTaxesMonthly,
            SalaryAfterTaxesYearlyTotal = salaryAfterTaxesMonthly.Sum(),
            TaxDifferenceMonthly = taxDifferenceMonthly,
            TaxDifferenceYearlyTotal = taxDifferenceMonthly.Sum(),
            DividendoJurosMonthly = dividendoJurosMonthly,
            DividendoJurosYearlyTotal = dividendoJurosMonthly.Sum()
        };
    }

    private static decimal?[] ComputeDiffs(decimal[] monthlyValues, decimal? januaryDiff)
    {
        var diffs = new decimal?[monthlyValues.Length];
        diffs[0] = januaryDiff;
        for (var month = 1; month < monthlyValues.Length; month++)
        {
            diffs[month] = monthlyValues[month] - monthlyValues[month - 1];
        }

        return diffs;
    }
}
