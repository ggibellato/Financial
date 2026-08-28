using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class HistoricAverageService : IHistoricAverageService
{
    private const int MonthsInYear = 12;
    public const int AverageDecimalPlaces = 2;
    private const string EntityType = "AnnualSummary";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<HistoricAverageService> _logger;

    public HistoricAverageService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<HistoricAverageService> logger, TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<CategoryAnnualAverageDTO> GetHistoricSummaryAverageFromYear(int year)
    {
        using var span = StartSpan("GetHistoricSummaryAverageFromYear");
        try
        {
            var incomeAverages = GetHistoricIncomeAverageFromYear(year);
            var categoryAverages = GetHistoricCategoriesAverageFromYear(year);
            var categories = _repository.GetCategories().ToList();
            categoryAverages = AddMissingCategories(categoryAverages, categories);
            AddCategoryTotal(incomeAverages, categoryAverages, categories);
            AddIncomeToFinalResult(incomeAverages, categoryAverages);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetHistoricSummaryAverageFromYear");
            return [.. categoryAverages];
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(HistoricAverageService), operationName, EntityType);
    }

    private static IList<CategoryAnnualAverageDTO> AddMissingCategories(
        IList<CategoryAnnualAverageDTO> categoryAverages, IReadOnlyList<Category> categories)
    {
        var result = new List<CategoryAnnualAverageDTO>();

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
                    yearAverage.AnnualAverages.Add(new CategoryAverageDTO
                    {
                        Category = category,
                        Value = 0m
                    });
                }
            }
            result.Add(new CategoryAnnualAverageDTO{
                Year = yearAverage.Year,
                AnnualAverages = yearAverage.AnnualAverages.OrderBy(c => orderIndex.GetValueOrDefault(c.Category, int.MaxValue)).ToList()
            });
        }
        return result;
    }

    private static void AddCategoryTotal(
        IList<IncomeAnnualAverageDTO> incomeAverages, IList<CategoryAnnualAverageDTO> categoryAverages, IReadOnlyList<Category> categories)
    {
        var investmentCategoryName = categories.FirstOrDefault(c => c.IsInvestment)?.Name;

        foreach (var yearAverage in categoryAverages)
        {
            var totalCategory = yearAverage.AnnualAverages.Sum(c => c.Value);
            var investmentCategory = yearAverage.AnnualAverages.FirstOrDefault(c => c.Category == investmentCategoryName)?.Value ?? 0m;

            var salaryAfterTaxes = incomeAverages.FirstOrDefault(i => i.Year == yearAverage.Year)?.SalaryAfterTaxesAverage ?? 0m;

            yearAverage.AnnualAverages.Add(new CategoryAverageDTO
            {
                Category = "Resultado (R-D-Inv)",
                Value = AnnualResultCalculator.ComputeResultado(salaryAfterTaxes, totalCategory, investmentCategory)
            });

            yearAverage.AnnualAverages.Add(new CategoryAverageDTO
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
        IList<CategoryAnnualAverageDTO> categoryAverages)
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

            var yearAverage = new CategoryAnnualAverageDTO
            {
                Year = incomeAverage.Year,
                AnnualAverages = new List<CategoryAverageDTO>()
            };
            InsertIncomeRows(yearAverage, incomeAverage);
            categoryAverages.Add(yearAverage);
        }
    }

    private static void InsertIncomeRows(CategoryAnnualAverageDTO yearAverage, IncomeAnnualAverageDTO? incomeAverage)
    {
        var salary = incomeAverage?.SalaryAverage ?? 0m;
        var salaryAfterTaxes = incomeAverage?.SalaryAfterTaxesAverage ?? 0m;

        yearAverage.AnnualAverages.Insert(0, new CategoryAverageDTO { Category = "Salary", Value = salary });
        yearAverage.AnnualAverages.Insert(1, new CategoryAverageDTO { Category = "Salary after taxes", Value = salaryAfterTaxes });
        yearAverage.AnnualAverages.Insert(2, new CategoryAverageDTO { Category = "Tax difference", Value = salary - salaryAfterTaxes });
        yearAverage.AnnualAverages.Insert(3, new CategoryAverageDTO
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
        var groupLookup = IncomeGroupLookupBuilder.Build(_repository.GetIncomeSources());
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
                var monthsElapsed = AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, yearGroup.Key);

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

    private IList<CategoryAnnualAverageDTO> GetHistoricCategoriesAverageFromYear(int year)
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
            var monthsElapsed = AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, yearGroup.Key);

            var annualAverages = yearGroup.Value.Select(categoryId =>
            {
                var series = MonthlySeries.FromMonthlyValues(Enumerable.Range(1, MonthsInYear)
                    .Select(month => sumByYearMonthCategory.GetValueOrDefault((yearGroup.Key, month, categoryId)))
                    .ToArray());

                return new CategoryAverageDTO
                {
                    Category = categoryNameById[categoryId],
                    Value = series.Average(monthsElapsed, AverageDecimalPlaces)
                };
            }).ToList();

            return new CategoryAnnualAverageDTO { Year = yearGroup.Key, AnnualAverages = annualAverages };
        });

        return [.. result.OrderByDescending(a => a.Year)];
    }
}
