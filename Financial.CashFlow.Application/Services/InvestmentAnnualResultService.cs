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

public sealed class InvestmentAnnualResultService : IInvestmentAnnualResultService
{
    private const int MonthsInYear = 12;
    private const string EntityType = "AnnualSummary";

    /// <summary>
    /// Investment averages/sums are intentionally never rounded (unlike the 2-decimal-place
    /// income/category averages), so this endpoint's values stay byte-identical to the
    /// pre-refactor investment-diffs output. Decimal division already caps at this many
    /// fractional digits, so rounding to it is a no-op in practice.
    /// </summary>
    private const int FullPrecisionDecimalPlaces = 28;

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<InvestmentAnnualResultService> _logger;

    public InvestmentAnnualResultService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<InvestmentAnnualResultService> logger, TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public InvestmentAnnualResultDTO GetInvestmentAnnualResultForYear(int year)
    {
        using var span = StartSpan("GetInvestmentAnnualResultForYear");
        try
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

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetInvestmentAnnualResultForYear");
            return new InvestmentAnnualResultDTO
            {
                Accounts = accounts.ToArray(),
                NetPosition = netPosition
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
        return _tracer.StartServiceSpan("CashFlow", nameof(InvestmentAnnualResultService), operationName, EntityType);
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
}
