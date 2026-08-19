using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Abstractions;

namespace Financial.CashFlow.Application.Services;

public sealed class TitheService : ITitheService
{
    private const decimal TithePercentage = 0.10m;
    private const string EntityType = "TitheSummary";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public TitheService(ICashFlowRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public TitheSummaryDTO GetTitheSummary(int year, int month)
    {
        using var span = StartSpan("GetTitheSummary");
        try
        {
            var titheBase = _repository.GetIncomes()
                .Where(i => i.Date.Year == year && i.Date.Month == month)
                .Sum(i => i.NetValue);

            var calculatedTithe = titheBase * TithePercentage;

            var dizimoTotal = _repository.GetExpenses()
                .Where(e => e.Date.Year == year && e.Date.Month == month && e.Category.IsTithe && e.CountsAsTithe)
                .Sum(e => e.Value);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return new TitheSummaryDTO
            {
                CalculatedTithe = calculatedTithe,
                TitheBalance = calculatedTithe - dizimoTotal
            };
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        var span = _tracer.StartSpan($"CashFlow.TitheService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }
}
