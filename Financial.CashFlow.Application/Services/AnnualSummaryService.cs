using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;

namespace Financial.CashFlow.Application.Services;

public sealed class AnnualSummaryService : IAnnualSummaryService
{
    private const int MonthsInYear = 12;
    public const int AverageDecimalPlaces = 2;

    /// <summary>
    /// Investment averages/sums are intentionally never rounded (unlike the 2-decimal-place
    /// income/category averages), so this endpoint's values stay byte-identical to the
    /// pre-refactor investment-diffs output. Decimal division already caps at this many
    /// fractional digits, so rounding to it is a no-op in practice.
    /// </summary>
    private const int FullPrecisionDecimalPlaces = 28;

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AnnualSummaryService(ICashFlowRepository repository, TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<CategoryAnnualTotalDTO> GetCategoryTotalsForYear(int year)
    {
        var monthsElapsed = NumberOfMonthsForAverage(year);

        return BuildCategoryTotalDtos(BuildAllCategorySeriesForYear(year), monthsElapsed);
    }

    private static List<CategoryAnnualTotalDTO> BuildCategoryTotalDtos(
        IEnumerable<(Category Category, MonthlySeries Display, MonthlySeries ForAverage)> categorySeries, int monthsElapsed) =>
        categorySeries
            .Select(c => new CategoryAnnualTotalDTO
            {
                Category = c.Category.Name,
                MonthlyTotals = c.Display.ToArray(),
                AnnualTotal = c.Display.Sum(),
                Average = c.ForAverage.Average(monthsElapsed, AverageDecimalPlaces)
            })
            .ToList();

    /// <summary>
    /// Builds each category's monthly series twice: <c>Display</c> includes every recorded
    /// expense for the year (so the in-progress current month's partial total still shows live
    /// in the table), while <c>ForAverage</c> excludes anything dated in the current calendar
    /// month - mirroring <see cref="GetHistoricCategoriesAverageFromYear"/>'s query-level cutoff -
    /// so a partially-elapsed month never inflates the numerator an "Average" figure divides by
    /// <see cref="NumberOfMonthsForAverage"/> completed months. Iterates every seeded category -
    /// active and inactive - so a since-deactivated category's historical totals remain complete.
    /// </summary>
    private IReadOnlyList<(Category Category, MonthlySeries Display, MonthlySeries ForAverage)> BuildAllCategorySeriesForYear(int year)
    {
        var yearExpenses = _repository.GetExpenses().Where(e => e.ReportingDate.Year == year).ToList();
        var now = _timeProvider.GetUtcNow();
        var currentMonthCutoff = new DateOnly(now.Year, now.Month, 1);

        var totalsByCategoryAndMonth = BuildCategoryMonthlyTotals(yearExpenses);
        var totalsForAverageByCategoryAndMonth = BuildCategoryMonthlyTotals(yearExpenses.Where(e => e.ReportingDate < currentMonthCutoff));

        return _repository.GetCategories()
            .Select(category => (
                category,
                Display: MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => totalsByCategoryAndMonth.GetValueOrDefault((category.Id, month)))
                    .ToArray()),
                ForAverage: MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => totalsForAverageByCategoryAndMonth.GetValueOrDefault((category.Id, month)))
                    .ToArray())))
            .ToList();
    }

    private static Dictionary<(Guid CategoryId, int Month), decimal> BuildCategoryMonthlyTotals(IEnumerable<Expense> expenses) =>
        expenses.GroupBy(e => (e.Category.Id, e.ReportingDate.Month)).ToDictionary(g => g.Key, g => g.Sum(e => e.Value));

    public InvestmentAnnualResultDTO GetInvestmentAnnualResultForYear(int year)
    {
        var series = ComputeInvestmentSeriesForYear(year);

        var accounts = series.AccountSeries
            .Select(a => new InvestmentAccountAnnualDiffDTO
            {
                Account = a.Account.Name,
                IsLiability = a.Account.IsLiability,
                MonthlyValues = a.MonthlyValues.ToArray(),
                MonthlyDiffs = a.Diffs.ToArray()
            })
            .ToList();

        var relevantDiffsSeries = MonthlySeries.FromMonthlyValues(Enumerable.Range(0, MonthsInYear)
            .Select(month => month < series.LastRelevantMonth ? series.NetPositionDiffs[month] ?? 0m : 0m)
            .ToArray());
        var monthsElapsed = series.NetPositionDiffs.Take(series.LastRelevantMonth).Count(d => d.HasValue);

        var netPosition = new NetPositionAnnualDiffDTO
        {
            MonthlyValues = series.NetPositionSeries.ToArray(),
            MonthlyDiffs = series.NetPositionDiffs.ToArray(),
            FullYearNetChange = series.NetPositionSeries[series.LastRelevantMonth - 1] - series.NetPositionSeries[0],
            AverageMonthResult = relevantDiffsSeries.Average(monthsElapsed, FullPrecisionDecimalPlaces),
            SumOfMonthResults = relevantDiffsSeries.Sum()
        };

        return new InvestmentAnnualResultDTO
        {
            Accounts = accounts.ToArray(),
            NetPosition = netPosition
        };
    }

    private (
        List<(InvestmentAccount Account, MonthlySeries MonthlyValues, IReadOnlyList<decimal?> Diffs)> AccountSeries,
        MonthlySeries NetPositionSeries,
        IReadOnlyList<decimal?> NetPositionDiffs,
        int LastRelevantMonth) ComputeInvestmentSeriesForYear(int year)
    {
        var allSnapshots = _repository.GetInvestmentSnapshots().ToList();
        var allAccounts = _repository.GetInvestmentAccounts().ToList();
        var now = _timeProvider.GetUtcNow();
        var scopedAccounts = YearScopedInvestmentAccountResolver.ResolveForYear(allAccounts, allSnapshots, year, now.Year);

        var valueByAccountAndMonth = allSnapshots
            .Where(s => s.Year == year)
            .GroupBy(s => (AccountId: s.Account.Id, s.Month))
            .ToDictionary(g => g.Key, g => g.First().Value);

        var priorYearDecemberByAccount = allSnapshots
            .Where(s => s.Year == year - 1 && s.Month == MonthsInYear)
            .GroupBy(s => s.Account.Id)
            .ToDictionary(g => g.Key, g => g.First().Value);

        var hasPriorYearData = allSnapshots.Any(s => s.Year == year - 1);

        var accountSeries = scopedAccounts
            .Select(account =>
            {
                var monthlyValues = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => valueByAccountAndMonth.GetValueOrDefault((AccountId: account.Id, month)))
                    .ToArray());

                decimal? priorClosingValue = hasPriorYearData
                    ? priorYearDecemberByAccount.GetValueOrDefault(account.Id, 0m)
                    : null;

                return (account, monthlyValues, diffs: (IReadOnlyList<decimal?>)monthlyValues.DiffsFrom(priorClosingValue));
            })
            .ToList();

        var netPositionSeries = accountSeries.Aggregate(MonthlySeries.Zero(), (net, a) =>
            net.Add(a.account.IsLiability
                ? MonthlySeries.FromMonthlyValues(a.monthlyValues.AsReadOnly().Select(v => -v).ToArray())
                : a.monthlyValues));

        decimal? netPositionPriorClosingValue = hasPriorYearData
            ? accountSeries.Sum(a => (a.account.IsLiability ? -1m : 1m) * priorYearDecemberByAccount.GetValueOrDefault(a.account.Id, 0m))
            : null;

        var netPositionDiffs = netPositionSeries.DiffsFrom(netPositionPriorClosingValue);

        var lastRelevantMonth = year >= now.Year ? Math.Min(now.Month, MonthsInYear) : MonthsInYear;

        return (accountSeries, netPositionSeries, netPositionDiffs, lastRelevantMonth);
    }

    public IncomeAnnualSummaryDTO GetIncomeSummaryForYear(int year)
    {
        var (display, forAverage) = BuildIncomeSeriesPairForYear(year);
        var monthsElapsed = NumberOfMonthsForAverage(year);

        return BuildIncomeSummaryDto(display, forAverage, monthsElapsed);
    }

    private static IncomeAnnualSummaryDTO BuildIncomeSummaryDto(IncomeSeries display, IncomeSeries forAverage, int monthsElapsed) => new()
    {
        SalaryMonthly = display.Salary.ToArray(),
        SalaryAnnualTotal = display.Salary.Sum(),
        SalaryAverage = forAverage.Salary.Average(monthsElapsed, AverageDecimalPlaces),
        SalaryAfterTaxesMonthly = display.SalaryAfterTaxes.ToArray(),
        SalaryAfterTaxesAnnualTotal = display.SalaryAfterTaxes.Sum(),
        SalaryAfterTaxesAverage = forAverage.SalaryAfterTaxes.Average(monthsElapsed, AverageDecimalPlaces),
        TaxDifferenceMonthly = display.TaxDifference.ToArray(),
        TaxDifferenceAnnualTotal = display.TaxDifference.Sum(),
        TaxDifferenceAverage = forAverage.TaxDifference.Average(monthsElapsed, AverageDecimalPlaces),
        DividendoJurosMonthly = display.DividendoJuros.ToArray(),
        DividendoJurosAnnualTotal = display.DividendoJuros.Sum(),
        DividendoJurosAverage = forAverage.DividendoJuros.Average(monthsElapsed, AverageDecimalPlaces)
    };

    /// <summary>
    /// Builds a case-insensitive name -&gt; IncomeGroup lookup from the seeded IncomeSource list.
    /// Built once per call site (not per income record) to keep group resolution O(1) per record
    /// instead of re-scanning the seeded list for every income. An income source name with no
    /// matching seeded record resolves to <see cref="IncomeGroup.NonReportable"/>.
    /// </summary>
    private Dictionary<string, IncomeGroup> BuildIncomeGroupLookup() =>
        _repository.GetIncomeSources().ToDictionary(s => s.Name, s => s.Group, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Builds the income monthly series twice for the same reason as <see cref="BuildAllCategorySeriesForYear"/>:
    /// <c>Display</c> includes every recorded income for the year, while <c>ForAverage</c> excludes
    /// anything dated in the current calendar month so a partially-elapsed month never inflates an
    /// "Average" figure's numerator.
    /// </summary>
    private (IncomeSeries Display, IncomeSeries ForAverage) BuildIncomeSeriesPairForYear(int year)
    {
        var yearIncomes = _repository.GetIncomes().Where(i => i.Date.Year == year).ToList();
        var now = _timeProvider.GetUtcNow();
        var currentMonthCutoff = new DateOnly(now.Year, now.Month, 1);
        var groupLookup = BuildIncomeGroupLookup();

        return (
            BuildIncomeSeries(yearIncomes, groupLookup),
            BuildIncomeSeries(yearIncomes.Where(i => i.Date < currentMonthCutoff), groupLookup));
    }

    private readonly record struct IncomeSeries(
        MonthlySeries Salary, MonthlySeries SalaryAfterTaxes, MonthlySeries TaxDifference, MonthlySeries DividendoJuros);

    private static IncomeSeries BuildIncomeSeries(IEnumerable<Income> incomes, IReadOnlyDictionary<string, IncomeGroup> groupLookup)
    {
        var salaryMonthly = new decimal[MonthsInYear];
        var salaryAfterTaxesMonthly = new decimal[MonthsInYear];
        var dividendoJurosMonthly = new decimal[MonthsInYear];

        foreach (var income in incomes)
        {
            var monthIndex = income.Date.Month - 1;

            switch (groupLookup.GetValueOrDefault(income.IncomeSource.Name, IncomeGroup.NonReportable))
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

        return new IncomeSeries(
            MonthlySeries.FromMonthlyValues(salaryMonthly),
            MonthlySeries.FromMonthlyValues(salaryAfterTaxesMonthly),
            MonthlySeries.FromMonthlyValues(taxDifferenceMonthly),
            MonthlySeries.FromMonthlyValues(dividendoJurosMonthly));
    }

    public CategoryTotalsAnnualDTO GetCategoryTotalsAnnualForYear(int year)
    {
        var monthsElapsed = NumberOfMonthsForAverage(year);

        var categorySeries = BuildAllCategorySeriesForYear(year);
        var categoryTotals = BuildCategoryTotalDtos(categorySeries, monthsElapsed);

        var (incomeDisplay, incomeForAverage) = BuildIncomeSeriesPairForYear(year);
        var incomeSummary = BuildIncomeSummaryDto(incomeDisplay, incomeForAverage, monthsElapsed);

        var totalDespesasSeries = categorySeries.Aggregate(MonthlySeries.Zero(), (total, c) => total.Add(c.Display));
        var totalDespesasForAverageSeries = categorySeries.Aggregate(MonthlySeries.Zero(), (total, c) => total.Add(c.ForAverage));

        var investimento = categorySeries.First(c => c.Category.IsInvestment);

        var resultadoSeries = AnnualResultCalculator.ComputeResultado(incomeDisplay.SalaryAfterTaxes, totalDespesasSeries, investimento.Display);
        var resultadoForAverageSeries = AnnualResultCalculator.ComputeResultado(incomeForAverage.SalaryAfterTaxes, totalDespesasForAverageSeries, investimento.ForAverage);

        return new CategoryTotalsAnnualDTO
        {
            CategoryTotals = categoryTotals,
            IncomeSummary = incomeSummary,
            TotalDespesasMonthly = totalDespesasSeries.ToArray(),
            TotalDespesasAnnualTotal = totalDespesasSeries.Sum(),
            TotalDespesasAverage = totalDespesasForAverageSeries.Average(monthsElapsed, AverageDecimalPlaces),
            ResultadoMonthly = resultadoSeries.ToArray(),
            ResultadoAnnualTotal = resultadoSeries.Sum(),
            ResultadoAverage = resultadoForAverageSeries.Average(monthsElapsed, AverageDecimalPlaces)
        };
    }

    public IReadOnlyList<CategoryAnnualGroupValueDTO> GetHistoricSummaryAverageFromYear(int year)
    {
        var incomeAverages = GetHistoricIncomeAverageFromYear(year);
        var categoryAverages = GetHistoricCategoriesAverageFromYear(year);
        var categories = _repository.GetCategories().ToList();
        categoryAverages = AddMissingCategories(categoryAverages, categories);
        AddCategoryTotal(incomeAverages, categoryAverages, categories);
        AddIncomeToFinalResult(incomeAverages, categoryAverages);
        return [.. categoryAverages];
    }

    private static IList<CategoryAnnualGroupValueDTO> AddMissingCategories(
        IList<CategoryAnnualGroupValueDTO> categoryAverages, IReadOnlyList<Category> categories)
    {
        var result = new List<CategoryAnnualGroupValueDTO>();

        // Preserves the enum's old declaration order, since CategoryMigrator seeds the 14
        // categories in that exact same order and this list reflects the seeded order as-is.
        var orderedCategoryNames = categories.Select(c => c.Name).ToList();
        var orderIndex = orderedCategoryNames
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        foreach (var yearAverage in categoryAverages)
        {
            foreach (var category in orderedCategoryNames)
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
                AnnualAverages = yearAverage.AnnualAverages.OrderBy(c => orderIndex.GetValueOrDefault(c.Category, int.MaxValue)).ToList()
            });
        }
        return result;
    }

    private static void AddCategoryTotal(
        IList<IncomeAnnualAverageDTO> incomeAverages, IList<CategoryAnnualGroupValueDTO> categoryAverages, IReadOnlyList<Category> categories)
    {
        var investmentCategoryName = categories.FirstOrDefault(c => c.IsInvestment)?.Name;

        foreach (var yearAverage in categoryAverages)
        {
            var totalCategory = yearAverage.AnnualAverages.Sum(c => c.Value);
            var investmentCategory = yearAverage.AnnualAverages.FirstOrDefault(c => c.Category == investmentCategoryName)?.Value ?? 0m;

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

    /// <summary>
    /// Inserts the 4 income-derived rows (Salary, Salary after taxes, Tax difference,
    /// Dividendo/Juros) into every year already present from expense data, defaulting to 0 when
    /// that year has no matching income average - mirroring how <see cref="AddMissingCategories"/>
    /// zero-fills a category with no expenses that year, rather than omitting the row entirely.
    /// A year with income but no expense/category data at all (so it isn't in
    /// <paramref name="categoryAverages"/> yet) still gets its own row, added separately below.
    /// </summary>
    private static void AddIncomeToFinalResult(IList<IncomeAnnualAverageDTO> incomeAverages,
        IList<CategoryAnnualGroupValueDTO> categoryAverages)
    {
        foreach (var yearAverage in categoryAverages)
        {
            var incomeAverage = incomeAverages.FirstOrDefault(i => i.Year == yearAverage.Year);
            InsertIncomeRows(yearAverage, incomeAverage);
        }

        foreach (var incomeAverage in incomeAverages)
        {
            if (categoryAverages.Any(c => c.Year == incomeAverage.Year))
            {
                continue;
            }

            var yearAverage = new CategoryAnnualGroupValueDTO
            {
                Year = incomeAverage.Year,
                AnnualAverages = new List<CategoryGroupValueDTO>()
            };
            InsertIncomeRows(yearAverage, incomeAverage);
            categoryAverages.Add(yearAverage);
        }
    }

    private static void InsertIncomeRows(CategoryAnnualGroupValueDTO yearAverage, IncomeAnnualAverageDTO? incomeAverage)
    {
        var salary = incomeAverage?.SalaryAverage ?? 0m;
        var salaryAfterTaxes = incomeAverage?.SalaryAfterTaxesAverage ?? 0m;

        yearAverage.AnnualAverages.Insert(0, new CategoryGroupValueDTO { Category = "Salary", Value = salary });
        yearAverage.AnnualAverages.Insert(1, new CategoryGroupValueDTO { Category = "Salary after taxes", Value = salaryAfterTaxes });
        yearAverage.AnnualAverages.Insert(2, new CategoryGroupValueDTO { Category = "Tax difference", Value = salary - salaryAfterTaxes });
        yearAverage.AnnualAverages.Insert(3, new CategoryGroupValueDTO
        {
            Category = "Dividendo/Juros",
            Value = incomeAverage?.DividendoJurosAverage ?? 0m
        });
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
        var now = _timeProvider.GetUtcNow();
        var groupLookup = BuildIncomeGroupLookup();
        IncomeGroup Group(Income income) => groupLookup.GetValueOrDefault(income.IncomeSource.Name, IncomeGroup.NonReportable);

        var incomes = _repository.GetIncomes()
            .Where(e => e.Date.Year <= year && e.Date < new DateOnly(now.Year, now.Month, 1))
            .ToList();

        var sumsByYearMonthGroup = incomes
            .GroupBy(e => (e.Date.Year, e.Date.Month, Group: Group(e)))
            .ToDictionary(g => g.Key, g => (Gross: g.Sum(e => e.GrossValue ?? 0m), Net: g.Sum(e => e.NetValue)));

        var groupsByYear = incomes
            .GroupBy(e => e.Date.Year)
            .ToDictionary(g => g.Key, g => g.Select(Group).Distinct().ToList());

        return groupsByYear.ToDictionary(
            yearGroup => yearGroup.Key,
            yearGroup =>
            {
                var monthsElapsed = NumberOfMonthsForAverage(yearGroup.Key);

                return yearGroup.Value.Select(group =>
                {
                    var grossSeries = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                        .Select(month => sumsByYearMonthGroup.GetValueOrDefault((yearGroup.Key, month, group)).Gross)
                        .ToArray());
                    var netSeries = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                        .Select(month => sumsByYearMonthGroup.GetValueOrDefault((yearGroup.Key, month, group)).Net)
                        .ToArray());

                    return new IncomeGroupValueDTO
                    {
                        IncomeGroup = group,
                        GrossAverageValue = grossSeries.Average(monthsElapsed, AverageDecimalPlaces),
                        NetAverageValue = netSeries.Average(monthsElapsed, AverageDecimalPlaces)
                    };
                }).ToList();
            });
    }

    private int NumberOfMonthsForAverage(int year)
    {
        var now = _timeProvider.GetUtcNow();
        return year switch
        {
            var y when y == now.Year => now.Month - 1,
            2017 => 11,
            _ => 12,
        };
    }

    private IList<CategoryAnnualGroupValueDTO> GetHistoricCategoriesAverageFromYear(int year)
    {
        var now = _timeProvider.GetUtcNow();
        var expenses = _repository.GetExpenses()
            .Where(e => e.ReportingDate.Year <= year && e.ReportingDate < new DateOnly(now.Year, now.Month, 1))
            .ToList();

        var sumByYearMonthCategory = expenses
            .GroupBy(e => (e.ReportingDate.Year, e.ReportingDate.Month, e.Category.Id))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Value));

        var categoryNameById = expenses
            .Select(e => e.Category)
            .DistinctBy(c => c.Id)
            .ToDictionary(c => c.Id, c => c.Name);

        var categoriesByYear = expenses
            .GroupBy(e => e.ReportingDate.Year)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Category.Id).Distinct().ToList());

        var result = categoriesByYear.Select(yearGroup =>
        {
            var monthsElapsed = NumberOfMonthsForAverage(yearGroup.Key);

            var annualAverages = yearGroup.Value.Select(categoryId =>
            {
                var series = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => sumByYearMonthCategory.GetValueOrDefault((yearGroup.Key, month, categoryId)))
                    .ToArray());

                return new CategoryGroupValueDTO
                {
                    Category = categoryNameById[categoryId],
                    Value = series.Average(monthsElapsed, AverageDecimalPlaces)
                };
            }).ToList();

            return new CategoryAnnualGroupValueDTO { Year = yearGroup.Key, AnnualAverages = annualAverages };
        });

        return [.. result.OrderByDescending(a => a.Year)];
    }
}
