using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class CategorySummaryService : ICategorySummaryService
{
    private const int MonthsInYear = 12;
    public const int AverageDecimalPlaces = 2;
    private const string EntityType = "AnnualSummary";

    private readonly ICashFlowRepository _repository;
    private readonly IIncomeSummaryService _incomeSummaryService;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CategorySummaryService> _logger;

    public CategorySummaryService(
        ICashFlowRepository repository,
        IIncomeSummaryService incomeSummaryService,
        ITelemetryTracer tracer,
        ILogger<CategorySummaryService> logger,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _incomeSummaryService = incomeSummaryService ?? throw new ArgumentNullException(nameof(incomeSummaryService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<CategoryAnnualTotalDTO> GetCategoryTotalsForYear(int year)
    {
        using var span = StartSpan("GetCategoryTotalsForYear");
        try
        {
            var monthsElapsed = AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(_timeProvider.GetUtcNow(), year);

            var result = BuildCategoryTotalDtos(BuildAllCategorySeriesForYear(year), monthsElapsed);
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCategoryTotalsForYear");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public CategoryTotalsAnnualDTO GetCategoryTotalsAnnualForYear(int year)
    {
        using var span = StartSpan("GetCategoryTotalsAnnualForYear");
        try
        {
            var monthsElapsed = AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(_timeProvider.GetUtcNow(), year);

            var categorySeries = BuildAllCategorySeriesForYear(year);
            var categoryTotals = BuildCategoryTotalDtos(categorySeries, monthsElapsed);

            var incomeSummary = _incomeSummaryService.GetIncomeSummaryForYear(year);
            var (incomeDisplaySalaryAfterTaxes, incomeForAverageSalaryAfterTaxes) = _incomeSummaryService.GetSalaryAfterTaxesSeriesForYear(year);

            var totalDespesasSeries = categorySeries.Aggregate(MonthlySeries.Zero(), (total, c) => total.Add(c.Display));
            var totalDespesasForAverageSeries = categorySeries.Aggregate(MonthlySeries.Zero(), (total, c) => total.Add(c.ForAverage));

            var investimento = categorySeries.First(c => c.Category.IsInvestment);

            var resultadoSeries = AnnualResultCalculator.ComputeResultado(incomeDisplaySalaryAfterTaxes, totalDespesasSeries, investimento.Display);
            var resultadoForAverageSeries = AnnualResultCalculator.ComputeResultado(incomeForAverageSalaryAfterTaxes, totalDespesasForAverageSeries, investimento.ForAverage);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCategoryTotalsAnnualForYear");
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
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CategorySummaryService), operationName, EntityType);
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
    /// month - so a partially-elapsed month never inflates the numerator an "Average" figure
    /// divides by completed months. Iterates every seeded category - active and inactive - so a
    /// since-deactivated category's historical totals remain complete.
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
}
