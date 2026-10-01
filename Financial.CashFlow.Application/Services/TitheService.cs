using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class TitheService : ITitheService
{
    private const string EntityType = "TitheSummary";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TitheService> _logger;

    public TitheService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<TitheService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TitheSummaryDTO> GetTitheSummaryAsync(int year, int month)
    {
        using var span = StartSpan("GetTitheSummaryAsync");
        try
        {
            var result = await ResolveMonthAsync(year, month).ConfigureAwait(false);

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

            var result = await ResolveMonthAsync(year, month).ConfigureAwait(false);

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

        var anchor = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        await _repository.ApplyAndSaveAsync(() =>
        {
            _repository.SetTitheCarryForwardEffectiveFrom(anchor);
            return true;
        }).ConfigureAwait(false);

        return anchor;
    }

    private async Task<TitheSummaryDTO> ResolveMonthAsync(int year, int month)
    {
        var anchor = await ResolveAnchorAsync().ConfigureAwait(false);
        return Resolve(year, month, anchor);
    }

    private TitheCarryForward? FindDecision(int year, int month) =>
        _repository.GetTitheCarryForwards().FirstOrDefault(d => d.Year == year && d.Month == month);

    private TitheSummaryDTO Resolve(int year, int month, DateOnly anchor)
    {
        var baseBalance = ComputeBaseBalance(year, month, out var calculatedTithe);
        var (prevYear, prevMonth) = PreviousMonth(year, month);

        var carryAmount = 0m;
        var included = false;
        if (new DateOnly(year, month, 1) > anchor)
        {
            var previousBalance = new DateOnly(prevYear, prevMonth, 1) <= anchor
                ? ComputeBaseBalance(prevYear, prevMonth, out _)
                : Resolve(prevYear, prevMonth, anchor).TitheBalance;

            if (previousBalance > 0)
            {
                carryAmount = previousBalance;
                included = FindDecision(year, month)?.Included ?? true;
            }
        }

        return new TitheSummaryDTO
        {
            CalculatedTithe = calculatedTithe,
            TitheBalance = baseBalance + (included ? carryAmount : 0m),
            CarryForward = carryAmount > 0
                ? new TitheCarryForwardDTO
                {
                    Amount = carryAmount,
                    Included = included,
                    FromYear = prevYear,
                    FromMonth = prevMonth
                }
                : null
        };
    }

    private decimal ComputeBaseBalance(int year, int month, out decimal calculatedTithe)
    {
        var titheBase = _repository.GetIncomes()
            .Where(i => i.Date.Year == year && i.Date.Month == month)
            .Sum(i => i.NetValue);

        calculatedTithe = TitheRule.CalculateTithe(titheBase);

        var dizimoTotal = _repository.GetExpenses()
            .Where(e => e.Date.Year == year && e.Date.Month == month && e.Category.IsTithe && e.CountsAsTithe)
            .Sum(e => e.Value);

        return calculatedTithe - dizimoTotal;
    }

    private static (int Year, int Month) PreviousMonth(int year, int month) =>
        month == 1 ? (year - 1, 12) : (year, month - 1);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(TitheService), operationName, EntityType);
    }
}
