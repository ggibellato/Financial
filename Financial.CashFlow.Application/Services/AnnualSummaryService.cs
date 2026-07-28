using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Services;

public sealed class AnnualSummaryService : IAnnualSummaryService
{
    private const int MonthsInYear = 12;
    private const string SalaryIncomeGroup = "Salary";
    private const int AverageDecimalPlaces = 2;

    private readonly ICashFlowRepository _repository;

    public AnnualSummaryService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<CategoryAnnualTotalDTO> GetCategoryTotalsForYear(int year)
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

                return new CategoryAnnualTotalDTO
                {
                    Category = category.ToString(),
                    MonthlyTotals = monthlyTotals,
                    AnnualTotal = monthlyTotals.Sum()
                };
            })
            .ToList();
    }

    public InvestmentDiffsAnnualDTO GetInvestmentDiffsForYear(int year)
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

                return new InvestmentAccountAnnualDiffDTO
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

        var netPosition = new NetPositionAnnualDiffDTO
        {
            MonthlyValues = netPositionValues,
            MonthlyDiffs = netPositionDiffs,
            FullYearNetChange = netPositionValues[lastRelevantMonth - 1] - netPositionValues[0],
            AverageMonthResult = relevantDiffs.Count > 0 ? relevantDiffs.Average() : 0m,
            SumOfMonthResults = relevantDiffs.Sum()
        };

        return new InvestmentDiffsAnnualDTO
        {
            Accounts = accounts.ToArray(),
            NetPosition = netPosition
        };
    }

    public IncomeAnnualSummaryDTO GetIncomeSummaryForYear(int year)
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

        return new IncomeAnnualSummaryDTO
        {
            SalaryMonthly = salaryMonthly,
            SalaryAnnualTotal = salaryMonthly.Sum(),
            SalaryAfterTaxesMonthly = salaryAfterTaxesMonthly,
            SalaryAfterTaxesAnnualTotal = salaryAfterTaxesMonthly.Sum(),
            TaxDifferenceMonthly = taxDifferenceMonthly,
            TaxDifferenceAnnualTotal = taxDifferenceMonthly.Sum(),
            DividendoJurosMonthly = dividendoJurosMonthly,
            DividendoJurosAnnualTotal = dividendoJurosMonthly.Sum()
        };
    }

    public IReadOnlyList<CategoryAnnualAverageDTO> GetHistoricSummaryAverageFromYear(int year)
    {
        var incomeAverages = GetHistoricIncomeAverageFromYear(year);
        var categoryAverages = GetHistoricCategoriesAverageFromYear(year);
        categoryAverages = AddMissingCategories(categoryAverages);
        AddCategoryTotal(incomeAverages, categoryAverages);
        AddIncomeToFinalResult(incomeAverages, categoryAverages);
        return [.. categoryAverages];
    }

    private IList<CategoryAnnualAverageDTO> AddMissingCategories(IList<CategoryAnnualAverageDTO> categoryAverages)
    {
        var result = new List<CategoryAnnualAverageDTO>();

        var uniqueListOfCategories = Enum.GetValues<Category>().Select(c => c.ToString()).ToList();

        foreach (var yearAverage in categoryAverages)
        {
            foreach (var category in uniqueListOfCategories)
            {
                if (!yearAverage.AnnualAverages.Any(c => c.Category == category))
                {
                    yearAverage.AnnualAverages.Add(new CategoryAverageDTO
                    {
                        Category = category,
                        Average = 0m
                    });
                }
            }
            result.Add(new CategoryAnnualAverageDTO{
                Year = yearAverage.Year,
                AnnualAverages = yearAverage.AnnualAverages.OrderBy(c => Enum.Parse<Category>(c.Category)).ToList()
            });
        }
        return result;
    }

    private void AddCategoryTotal(IList<IncomeAnnualAverageDTO> incomeAverages, IList<CategoryAnnualAverageDTO> categoryAverages)
    {
        foreach (var yearAverage in categoryAverages)
        {
            var totalCategory = yearAverage.AnnualAverages.Sum(c => c.Average);
            var investmentCategory = yearAverage.AnnualAverages.FirstOrDefault(c => c.Category == "Investimento")?.Average ?? 0m;

            var salaryAfterTaxes = incomeAverages.FirstOrDefault(i => i.Year == yearAverage.Year)?.SalaryAfterTaxesAverage ?? 0m;

            yearAverage.AnnualAverages.Add(new CategoryAverageDTO
            {
                Category = "Resultado (R-D-Inv)",
                Average = salaryAfterTaxes - totalCategory + investmentCategory
            });

            yearAverage.AnnualAverages.Add(new CategoryAverageDTO
            {
                Category = "Total despesas",
                Average = totalCategory
            });
        }
    }

    private static void AddIncomeToFinalResult(IList<IncomeAnnualAverageDTO> incomeAverages, 
        IList<CategoryAnnualAverageDTO> categoryAverages)
    {
        foreach (var incomeAverage in incomeAverages)
        {
            var yearAverage = categoryAverages.FirstOrDefault(c => c.Year == incomeAverage.Year);
            if (yearAverage is null)
            {
                yearAverage = new CategoryAnnualAverageDTO
                {
                    Year = incomeAverage.Year,
                    AnnualAverages = new List<CategoryAverageDTO>()
                };
                categoryAverages.Add(yearAverage);
            }

            yearAverage.AnnualAverages.Insert(0, new CategoryAverageDTO
            {
                Category = "Salary",
                Average = incomeAverage.SalaryAverage
            });
            yearAverage.AnnualAverages.Insert(1, new CategoryAverageDTO
            {
                Category = "Salary after taxes",
                Average = incomeAverage.SalaryAfterTaxesAverage
            });
            yearAverage.AnnualAverages.Insert(2, new CategoryAverageDTO
            {
                Category = "Tax difference",
                Average = incomeAverage.SalaryAverage - incomeAverage.SalaryAfterTaxesAverage
            });
            yearAverage.AnnualAverages.Insert(3, new CategoryAverageDTO
            {
                Category = "Dividendo/Juros",
                Average = incomeAverage.DividendoJurosAverage
            });
        }
    }

    private IList<IncomeAnnualAverageDTO> GetHistoricIncomeAverageFromYear(int year)
    {
        Dictionary<int, List<IncomeAverageDTO>> averageIncome = GetAnnualAverageIncomeByGroupIncome(year);
        Dictionary<int, IncomeAnnualAverageDTO> result = BuildAnnualIncomeAverages(averageIncome);
        return result.Values.OrderByDescending(a => a.Year).ToList();
    }

    private static Dictionary<int, IncomeAnnualAverageDTO> BuildAnnualIncomeAverages(Dictionary<int, List<IncomeAverageDTO>> averageIncomeByYear)
    {
        var result = new Dictionary<int, IncomeAnnualAverageDTO>();
        foreach (var incomeYear in averageIncomeByYear)
        {
            var (salary, salaryAfterTaxes, dividendoJuros) = CalculateAnnualIncomeDetails(incomeYear);
            result.Add(incomeYear.Key, new IncomeAnnualAverageDTO
            {
                Year = incomeYear.Key,
                SalaryAverage = salary,
                SalaryAfterTaxesAverage = salaryAfterTaxes,
                TaxDifferenceAverage = salary - salaryAfterTaxes,
                DividendoJurosAverage = dividendoJuros
            });
        }
        return result;
    }

    private static (decimal salary, decimal salaryAfterTaxes, decimal dividendoJuros)
        CalculateAnnualIncomeDetails(KeyValuePair<int, List<IncomeAverageDTO>> incomeYear)
    {
        var salary = 0m;
        var salaryAfterTaxes = 0m;
        var dividendoJuros = 0m;
        foreach (var incomeAverage in incomeYear.Value)
        {
            if (incomeAverage.IncomeGroup == SalaryIncomeGroup)
            {
                salary += incomeAverage.GrossAverageValue ?? 0m;
                salaryAfterTaxes += incomeAverage.NetAverageValue;
            }
            else if (incomeAverage.IncomeGroup == IncomeSource.DividendoJuros.ToString())
            {
                dividendoJuros += incomeAverage.NetAverageValue;
            }
        }
        return (salary, salaryAfterTaxes, dividendoJuros);
    }

    private Dictionary<int, List<IncomeAverageDTO>> GetAnnualAverageIncomeByGroupIncome(int year)
    {
        var relevantIncomes = _repository.GetIncomes()
            .Where(e => e.Date.Year <= year && e.Date < new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1))
            .ToList();

        return relevantIncomes
            .Select(e => e.Date.Year)
            .Distinct()
            .ToDictionary(incomeYear => incomeYear, incomeYear =>
            {
                var monthsToAverage = GetMonthsToAverage(incomeYear);
                return relevantIncomes
                    .Where(e => e.Date.Year == incomeYear)
                    .GroupBy(e => GetIncomeGroup(e.IncomeSource))
                    .Select(incomeGroup =>
                    {
                        var grossMonthly = new decimal[MonthsInYear];
                        var netMonthly = new decimal[MonthsInYear];
                        foreach (var income in incomeGroup)
                        {
                            grossMonthly[income.Date.Month - 1] += income.GrossValue ?? 0m;
                            netMonthly[income.Date.Month - 1] += income.NetValue;
                        }

                        return new IncomeAverageDTO
                        {
                            IncomeGroup = incomeGroup.Key,
                            GrossAverageValue = Math.Round(AverageOverMonths(grossMonthly, monthsToAverage), AverageDecimalPlaces),
                            NetAverageValue = Math.Round(AverageOverMonths(netMonthly, monthsToAverage), AverageDecimalPlaces)
                        };
                    })
                    .ToList();
            });
    }

    private static string GetIncomeGroup(IncomeSource incomeSource) =>
        incomeSource is IncomeSource.Gleison or IncomeSource.Ariana
            ? SalaryIncomeGroup
            : IncomeSource.DividendoJuros.ToString();

    private IList<CategoryAnnualAverageDTO> GetHistoricCategoriesAverageFromYear(int year)
    {
        var relevantExpenses = _repository.GetExpenses()
            .Where(e => e.Date.Year <= year && e.Date < new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1))
            .ToList();

        return relevantExpenses
            .Select(e => e.Date.Year)
            .Distinct()
            .OrderByDescending(expenseYear => expenseYear)
            .Select(expenseYear =>
            {
                var monthsToAverage = GetMonthsToAverage(expenseYear);
                return new CategoryAnnualAverageDTO
                {
                    Year = expenseYear,
                    AnnualAverages = relevantExpenses
                        .Where(e => e.Date.Year == expenseYear)
                        .GroupBy(e => e.Category)
                        .Select(categoryGroup =>
                        {
                            var monthlyTotals = new decimal[MonthsInYear];
                            foreach (var expense in categoryGroup)
                            {
                                monthlyTotals[expense.Date.Month - 1] += expense.Value;
                            }

                            return new CategoryAverageDTO
                            {
                                Category = categoryGroup.Key.ToString(),
                                Average = Math.Round(AverageOverMonths(monthlyTotals, monthsToAverage), AverageDecimalPlaces)
                            };
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    // A closed/past year always averages over all 12 calendar months - a month with no recorded
    // movement still counts as a real elapsed month, matching the source spreadsheet's own
    // AVERAGE(B:M) over always-filled cells. The current calendar year instead only averages over
    // its fully completed months (current month - 1): the in-progress month is excluded entirely
    // (never treated as a completed zero month), and a future year has no completed months at all.
    private static int GetMonthsToAverage(int year) =>
        year < DateTime.Now.Year ? MonthsInYear
        : year == DateTime.Now.Year ? DateTime.Now.Month - 1
        : 0;

    private static decimal AverageOverMonths(decimal[] monthlyTotals, int monthsToAverage) =>
        monthsToAverage == 0 ? 0m : monthlyTotals.Take(monthsToAverage).Sum() / monthsToAverage;

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
