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

public sealed class IncomeSummaryService : IIncomeSummaryService
{
    private const int MonthsInYear = 12;
    public const int AverageDecimalPlaces = 2;
    private const string EntityType = "AnnualSummary";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<IncomeSummaryService> _logger;

    public IncomeSummaryService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<IncomeSummaryService> logger, TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IncomeAnnualSummaryDTO GetIncomeSummaryForYear(int year)
    {
        using var span = StartSpan("GetIncomeSummaryForYear");
        try
        {
            var (display, forAverage) = BuildIncomeSeriesPairForYear(year);
            var monthsElapsed = AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(_timeProvider.GetUtcNow(), year);

            var result = BuildIncomeSummaryDto(display, forAverage, monthsElapsed);
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetIncomeSummaryForYear");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public (MonthlySeries Display, MonthlySeries ForAverage) GetSalaryAfterTaxesSeriesForYear(int year)
    {
        using var span = StartSpan("GetSalaryAfterTaxesSeriesForYear");
        try
        {
            var (display, forAverage) = BuildIncomeSeriesPairForYear(year);
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetSalaryAfterTaxesSeriesForYear");
            return (display.SalaryAfterTaxes, forAverage.SalaryAfterTaxes);
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
        return _tracer.StartServiceSpan("CashFlow", nameof(IncomeSummaryService), operationName, EntityType);
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
    /// Builds the income monthly series twice: <c>Display</c> includes every recorded income for
    /// the year, while <c>ForAverage</c> excludes anything dated in the current calendar month so
    /// a partially-elapsed month never inflates an "Average" figure's numerator.
    /// </summary>
    private (IncomeSeries Display, IncomeSeries ForAverage) BuildIncomeSeriesPairForYear(int year)
    {
        var yearIncomes = _repository.GetIncomes().Where(i => i.Date.Year == year).ToList();
        var now = _timeProvider.GetUtcNow();
        var currentMonthCutoff = new DateOnly(now.Year, now.Month, 1);
        var groupLookup = IncomeGroupLookupBuilder.Build(_repository.GetIncomeSources());

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
}
