using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;

namespace Financial.CashFlow.Application.Services;

public sealed class AnnualSummaryService : IAnnualSummaryService
{
    private const int MonthsInYear = 12;
    public const int AverageDecimalPlaces = 2;

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
                var monthlyTotals = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => totalsByCategoryAndMonth.GetValueOrDefault((category, month)))
                    .ToArray());

                return new CategoryAnnualTotalDTO
                {
                    Category = category.ToString(),
                    MonthlyTotals = monthlyTotals.AsReadOnly().ToArray(),
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

        var accountSeries = scopedAccounts
            .Select(account =>
            {
                var monthlyValues = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => valueByAccountAndMonth.GetValueOrDefault((account.Name, month)))
                    .ToArray());

                decimal? priorClosingValue = hasPriorYearData
                    ? priorYearDecemberByAccount.GetValueOrDefault(account.Name, 0m)
                    : null;

                return (account, monthlyValues, diffs: monthlyValues.DiffsFrom(priorClosingValue));
            })
            .ToList();

        var accounts = accountSeries
            .Select(a => new InvestmentAccountAnnualDiffDTO
            {
                Account = a.account.Name,
                IsLiability = a.account.IsLiability,
                MonthlyValues = a.monthlyValues.AsReadOnly().ToArray(),
                MonthlyDiffs = a.diffs.ToArray()
            })
            .ToList();

        var netPositionSeries = accountSeries.Aggregate(MonthlySeries.Zero(), (net, a) =>
            net.Add(a.account.IsLiability
                ? MonthlySeries.FromMonthlyValues(a.monthlyValues.AsReadOnly().Select(v => -v).ToArray())
                : a.monthlyValues));

        decimal? netPositionPriorClosingValue = hasPriorYearData
            ? accountSeries.Sum(a => (a.account.IsLiability ? -1m : 1m) * priorYearDecemberByAccount.GetValueOrDefault(a.account.Name, 0m))
            : null;

        var netPositionDiffs = netPositionSeries.DiffsFrom(netPositionPriorClosingValue);

        var lastRelevantMonth = year >= DateTime.Now.Year ? Math.Min(DateTime.Now.Month, MonthsInYear) : MonthsInYear;
        var relevantDiffs = netPositionDiffs.Take(lastRelevantMonth).Where(d => d.HasValue).Select(d => d!.Value).ToList();

        var netPosition = new NetPositionAnnualDiffDTO
        {
            MonthlyValues = netPositionSeries.AsReadOnly().ToArray(),
            MonthlyDiffs = netPositionDiffs.ToArray(),
            FullYearNetChange = netPositionSeries[lastRelevantMonth - 1] - netPositionSeries[0],
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

            switch (income.Group)
            {
                case IncomeGroup.Salary:
                    salaryMonthly[monthIndex] += income.GrossValue ?? 0m;
                    salaryAfterTaxesMonthly[monthIndex] += income.NetValue;
                    break;
                case IncomeGroup.DividendoJuros:
                    dividendoJurosMonthly[monthIndex] += income.NetValue;
                    break;
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

    public IReadOnlyList<CategoryAnnualGroupValueDTO> GetHistoricSummaryAverageFromYear(int year)
    {
        var incomeAverages = GetHistoricIncomeAverageFromYear(year);
        var categoryAverages = GetHistoricCategoriesAverageFromYear(year);
        categoryAverages = AddMissingCategories(categoryAverages);
        AddCategoryTotal(incomeAverages, categoryAverages);
        AddIncomeToFinalResult(incomeAverages, categoryAverages);
        return [.. categoryAverages];
    }

    private IList<CategoryAnnualGroupValueDTO> AddMissingCategories(IList<CategoryAnnualGroupValueDTO> categoryAverages)
    {
        var result = new List<CategoryAnnualGroupValueDTO>();

        var uniqueListOfCategories = Enum.GetValues<Category>().Select(c => c.ToString()).ToList();

        foreach (var yearAverage in categoryAverages)
        {
            foreach (var category in uniqueListOfCategories)
            {
                if (!yearAverage.AnnualAverages.Any(c => c.Category == category))
                {
                    yearAverage.AnnualAverages.Add(new CategoryGroupValueDTO
                    {
                        Category = category,
                        Value = 0m
                    });
                }
            }
            result.Add(new CategoryAnnualGroupValueDTO{
                Year = yearAverage.Year,
                AnnualAverages = yearAverage.AnnualAverages.OrderBy(c => Enum.Parse<Category>(c.Category)).ToList()
            });
        }
        return result;
    }

    private void AddCategoryTotal(IList<IncomeAnnualAverageDTO> incomeAverages, IList<CategoryAnnualGroupValueDTO> categoryAverages)
    {
        foreach (var yearAverage in categoryAverages)
        {
            var totalCategory = yearAverage.AnnualAverages.Sum(c => c.Value);
            var investmentCategory = yearAverage.AnnualAverages.FirstOrDefault(c => c.Category == nameof(Category.Investimento))?.Value ?? 0m;

            var salaryAfterTaxes = incomeAverages.FirstOrDefault(i => i.Year == yearAverage.Year)?.SalaryAfterTaxesAverage ?? 0m;

            yearAverage.AnnualAverages.Add(new CategoryGroupValueDTO
            {
                Category = "Resultado (R-D-Inv)",
                Value = AnnualResultCalculator.ComputeResultado(salaryAfterTaxes, totalCategory, investmentCategory)
            });

            yearAverage.AnnualAverages.Add(new CategoryGroupValueDTO
            {
                Category = "Total despesas",
                Value = totalCategory
            });
        }
    }

    private static void AddIncomeToFinalResult(IList<IncomeAnnualAverageDTO> incomeAverages, 
        IList<CategoryAnnualGroupValueDTO> categoryAverages)
    {
        foreach (var incomeAverage in incomeAverages)
        {
            var yearAverage = categoryAverages.FirstOrDefault(c => c.Year == incomeAverage.Year);
            if (yearAverage is null)
            {
                yearAverage = new CategoryAnnualGroupValueDTO
                {
                    Year = incomeAverage.Year,
                    AnnualAverages = new List<CategoryGroupValueDTO>()
                };
                categoryAverages.Add(yearAverage);
            }

            yearAverage.AnnualAverages.Insert(0, new CategoryGroupValueDTO
            {
                Category = "Salary",
                Value = incomeAverage.SalaryAverage
            });
            yearAverage.AnnualAverages.Insert(1, new CategoryGroupValueDTO
            {
                Category = "Salary after taxes",
                Value = incomeAverage.SalaryAfterTaxesAverage
            });
            yearAverage.AnnualAverages.Insert(2, new CategoryGroupValueDTO
            {
                Category = "Tax difference",
                Value = incomeAverage.SalaryAverage - incomeAverage.SalaryAfterTaxesAverage
            });
            yearAverage.AnnualAverages.Insert(3, new CategoryGroupValueDTO
            {
                Category = "Dividendo/Juros",
                Value = incomeAverage.DividendoJurosAverage
            });
        }
    }

    private IList<IncomeAnnualAverageDTO> GetHistoricIncomeAverageFromYear(int year)
    {
        Dictionary<int, List<IncomeGroupValueDTO>> averageIncome = GetAnnualAverageIncomeByGroupIncome(year);
        Dictionary<int, IncomeAnnualAverageDTO> result = BuildAnnualIncomeAverages(averageIncome);
        return result.Values.OrderByDescending(a => a.Year).ToList();
    }

    private static Dictionary<int, IncomeAnnualAverageDTO> BuildAnnualIncomeAverages(Dictionary<int, List<IncomeGroupValueDTO>> averageIncomeByYear)
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
        CalculateAnnualIncomeDetails(KeyValuePair<int, List<IncomeGroupValueDTO>> incomeYear)
    {
        var salary = 0m;
        var salaryAfterTaxes = 0m;
        var dividendoJuros = 0m;
        foreach (var incomeAverage in incomeYear.Value)
        {
            if (incomeAverage.IncomeGroup == IncomeGroup.Salary)
            {
                salary += incomeAverage.GrossAverageValue ?? 0m;
                salaryAfterTaxes += incomeAverage.NetAverageValue;
            }
            else if (incomeAverage.IncomeGroup == IncomeGroup.DividendoJuros)
            {
                dividendoJuros += incomeAverage.NetAverageValue;
            }
        }
        return (salary, salaryAfterTaxes, dividendoJuros);
    }

    private Dictionary<int, List<IncomeGroupValueDTO>> GetAnnualAverageIncomeByGroupIncome(int year)
    {
        var monthlySumByIncomeSource = _repository.GetIncomes()
            .Where(e => e.Date.Year <= year && e.Date < new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1))
            .GroupBy(e => new { e.Date.Year, e.Date.Month, IncomeGroup = e.Group })
            .Select(g => new
            {
                Key = (g.Key.Year, g.Key.Month, g.Key.IncomeGroup),
                GrossSumValue = g.Sum(e => e.GrossValue ?? 0m),
                NetSumValue = g.Sum(e => e.NetValue)
            });
        var averageExpenseByYear = monthlySumByIncomeSource
            .GroupBy(e => e.Key.Year)
            .ToDictionary(g => g.Key, g => g.GroupBy(e => e.Key.IncomeGroup)
                .Select(a => new IncomeGroupValueDTO
                {
                    IncomeGroup = a.Key,
                    GrossAverageValue = Math.Round(a.Sum(e => e.GrossSumValue)/NumberOfMonthsForAverage(g.Key), AverageDecimalPlaces),
                    NetAverageValue = Math.Round(a.Sum(e => e.NetSumValue)/NumberOfMonthsForAverage(g.Key), AverageDecimalPlaces)
                }).ToList());
        return averageExpenseByYear;
    }

    private decimal NumberOfMonthsForAverage(int year) => year switch
          {
            var y when y == DateTime.UtcNow.Year => DateTime.UtcNow.Month - 1,
            2017 => 11,
            _ => 12,
          };

    private IList<CategoryAnnualGroupValueDTO> GetHistoricCategoriesAverageFromYear(int year)
    {
        var monthlySumByCategory = _repository.GetExpenses()
            .Where(e => e.Date.Year <= year && e.Date < new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1))
            .GroupBy(e => (e.Date.Year, e.Date.Month, e.Category))
            .Select(g => new
            {
                Key = (g.Key.Year, g.Key.Month, g.Key.Category),
                Sum = g.Sum(e => e.Value)
            });
        var averageExpenseByYear = monthlySumByCategory
            .GroupBy(g => g.Key.Year)
            .Select(yearGroup => new CategoryAnnualGroupValueDTO
            {
                Year = yearGroup.Key,
                AnnualAverages =
                [
                    .. yearGroup
                    .GroupBy(g => g.Key.Category)
                    .Select(categoryGroup => new CategoryGroupValueDTO
                    {
                        Category = categoryGroup.Key.ToString(),
                        Value = Math.Round(categoryGroup.Sum(monthGroup => monthGroup.Sum)/NumberOfMonthsForAverage(yearGroup.Key), AverageDecimalPlaces)
                    })
                ]
            });
        return [.. averageExpenseByYear.OrderByDescending(a => a.Year)];
    }
}
