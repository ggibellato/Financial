using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeSourceService : IIncomeSourceService
{
    private const string EntityType = "IncomeSource";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public IncomeSourceService(ICashFlowRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public IReadOnlyList<IncomeSourceDTO> GetIncomeSources()
    {
        using var span = StartSpan("GetIncomeSources");
        try
        {
            var result = _repository.GetIncomeSources().Select(ToDto).ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return result;
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
        var span = _tracer.StartSpan($"CashFlow.IncomeSourceService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private static IncomeSourceDTO ToDto(IncomeSource source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        IsActive = source.IsActive,
        Group = source.Group.ToString()
    };
}
