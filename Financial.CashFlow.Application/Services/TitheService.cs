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

            var record = _repository.GetTitheCarryForwards().FirstOrDefault(d => d.Year == year && d.Month == month);
            if (record is null)
            {
                throw new ArgumentException($"No carry-forward is available for {year}-{month:D2}.");
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                record.SetIncluded(included);
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

    /// <summary>
    /// Resolves a month's Tithe figures, lazily anchoring <see cref="ICashFlowRepository.GetTitheCarryForwardEffectiveFrom"/>
    /// on the very first call and lazily walking back through any unresolved earlier months to
    /// snapshot the cascading carry-forward chain, before persisting everything in a single save.
    /// </summary>
    private async Task<TitheSummaryDTO> ResolveMonthAsync(int year, int month)
    {
        var effectiveFrom = _repository.GetTitheCarryForwardEffectiveFrom();
        var newEffectiveFrom = effectiveFrom is null
            ? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
            : (DateOnly?)null;
        var anchor = effectiveFrom ?? newEffectiveFrom!.Value;

        var pendingNew = new Dictionary<(int Year, int Month), TitheCarryForward>();
        var summary = Resolve(year, month, anchor, pendingNew);

        if (newEffectiveFrom is not null || pendingNew.Count > 0)
        {
            await _repository.ApplyAndSaveAsync(() =>
            {
                if (newEffectiveFrom is not null)
                {
                    _repository.SetTitheCarryForwardEffectiveFrom(newEffectiveFrom.Value);
                }

                foreach (var record in pendingNew.Values)
                {
                    _repository.AddTitheCarryForward(record);
                }

                return true;
            }).ConfigureAwait(false);
        }

        return summary;
    }

    private TitheSummaryDTO Resolve(
        int year, int month, DateOnly anchor, Dictionary<(int Year, int Month), TitheCarryForward> pendingNew)
    {
        var baseBalance = ComputeBaseBalance(year, month, out var calculatedTithe);
        var target = new DateOnly(year, month, 1);
        var (prevYear, prevMonth) = PreviousMonth(year, month);

        var record = _repository.GetTitheCarryForwards().FirstOrDefault(d => d.Year == year && d.Month == month);
        if (record is null && pendingNew.TryGetValue((year, month), out var pending))
        {
            record = pending;
        }

        if (record is null && target > anchor)
        {
            var prevTarget = new DateOnly(prevYear, prevMonth, 1);
            var predecessorAdjusted = prevTarget <= anchor
                ? ComputeBaseBalance(prevYear, prevMonth, out _)
                : Resolve(prevYear, prevMonth, anchor, pendingNew).TitheBalance;

            if (predecessorAdjusted > 0)
            {
                record = TitheCarryForward.Create(year, month, predecessorAdjusted);
                pendingNew[(year, month)] = record;
            }
        }

        var titheBalance = baseBalance + (record is { Included: true } ? record.Amount : 0m);

        return new TitheSummaryDTO
        {
            CalculatedTithe = calculatedTithe,
            TitheBalance = titheBalance,
            CarryForward = record is null
                ? null
                : new TitheCarryForwardDTO
                {
                    Amount = record.Amount,
                    Included = record.Included,
                    FromYear = prevYear,
                    FromMonth = prevMonth
                }
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
