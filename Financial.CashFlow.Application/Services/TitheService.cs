using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class TitheService : ITitheService
{
    private const string EntityType = "TitheSummary";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TitheService> _logger;

    public TitheService(ICashFlowRepository repository, TimeProvider timeProvider, ITelemetryTracer tracer, ILogger<TitheService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TitheSummaryDTO> GetTitheSummaryAsync(int year, int month)
    {
        using var span = StartSpan("GetTitheSummaryAsync");
        try
        {
            var anchor = await ResolveAnchorAsync().ConfigureAwait(false);
            var result = Resolve(year, month, anchor);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTitheSummaryAsync");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<IReadOnlyList<TitheMonthDTO>> GetTitheYearAsync(int year)
    {
        using var span = StartSpan("GetTitheYearAsync");
        try
        {
            if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be between 1 and 9999.");
            }

            var anchor = await ResolveAnchorAsync().ConfigureAwait(false);
            var result = ResolveMonths(new DateOnly(year, 1, 1), new DateOnly(year, 12, 1), anchor);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTitheYearAsync");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<TitheSummaryDTO> UpdateCarryForwardInclusionAsync(int year, int month, bool included)
    {
        using var span = StartSpan("UpdateCarryForwardInclusionAsync");
        try
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentException("Month must be between 1 and 12.", nameof(month));
            }

            var anchor = await ResolveAnchorAsync().ConfigureAwait(false);
            if (Resolve(year, month, anchor).CarryForward is null)
            {
                throw new ArgumentException($"No carry-forward is available for {year}-{month:D2}.");
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                var record = FindDecision(year, month);
                if (record is null)
                {
                    _repository.AddTitheCarryForward(TitheCarryForward.Create(year, month, included));
                }
                else
                {
                    record.SetIncluded(included);
                }

                return true;
            }).ConfigureAwait(false);

            var result = Resolve(year, month, anchor);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateCarryForwardInclusionAsync");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private async Task<DateOnly> ResolveAnchorAsync()
    {
        var effectiveFrom = _repository.GetTitheCarryForwardEffectiveFrom();
        if (effectiveFrom is not null)
        {
            return effectiveFrom.Value;
        }

        var today = _timeProvider.GetLocalToday();
        var anchor = new DateOnly(today.Year, today.Month, 1);
        await _repository.ApplyAndSaveAsync(() =>
        {
            _repository.SetTitheCarryForwardEffectiveFrom(anchor);
            return true;
        }).ConfigureAwait(false);

        return anchor;
    }

    private TitheCarryForward? FindDecision(int year, int month) =>
        _repository.GetTitheCarryForwards().FirstOrDefault(d => d.Year == year && d.Month == month);

    private TitheSummaryDTO Resolve(int year, int month, DateOnly anchor)
    {
        var target = new DateOnly(year, month, 1);
        return ResolveMonths(target, target, anchor)[0].Summary;
    }

    private List<TitheMonthDTO> ResolveMonths(DateOnly first, DateOnly last, DateOnly anchor)
    {
        var anchorMonth = new DateOnly(anchor.Year, anchor.Month, 1);
        var incomeBases = _repository.GetIncomes()
            .GroupBy(i => new DateOnly(i.Date.Year, i.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.NetValue));
        var tithePayments = _repository.GetExpenses()
            .Where(e => e.Category.IsTithe && e.CountsAsTithe)
            .GroupBy(e => new DateOnly(e.ReportingDate.Year, e.ReportingDate.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Value));
        var decisions = _repository.GetTitheCarryForwards()
            .DistinctBy(d => (d.Year, d.Month))
            .ToDictionary(d => new DateOnly(d.Year, d.Month, 1), d => d.Included);

        var start = first > anchorMonth ? anchorMonth : first;
        var count = (last.Year - start.Year) * 12 + last.Month - start.Month + 1;
        var months = new List<TitheMonthDTO>();
        var previousBalance = 0m;
        for (var i = 0; i < count; i++)
        {
            var month = start.AddMonths(i);
            var incomeBase = incomeBases.GetValueOrDefault(month);
            var calculatedTithe = TitheRule.CalculateTithe(incomeBase);
            var tithePaid = tithePayments.GetValueOrDefault(month);
            var carryAmount = month > anchorMonth && previousBalance > 0 ? previousBalance : 0m;
            var included = carryAmount > 0 && decisions.GetValueOrDefault(month, true);
            previousBalance = calculatedTithe - tithePaid + (included ? carryAmount : 0m);

            if (month >= first)
            {
                months.Add(new TitheMonthDTO
                {
                    Year = month.Year,
                    Month = month.Month,
                    Summary = new TitheSummaryDTO
                    {
                        IncomeBase = incomeBase,
                        CalculatedTithe = calculatedTithe,
                        TithePaid = tithePaid,
                        TitheBalance = previousBalance,
                        CarryForward = carryAmount > 0 ? CarryIn(month, carryAmount, included) : null
                    }
                });
            }
        }

        return months;
    }

    private static TitheCarryForwardDTO CarryIn(DateOnly month, decimal amount, bool included)
    {
        var from = month.AddMonths(-1);
        return new TitheCarryForwardDTO { Amount = amount, Included = included, FromYear = from.Year, FromMonth = from.Month };
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(TitheService), operationName, EntityType);
    }
}
