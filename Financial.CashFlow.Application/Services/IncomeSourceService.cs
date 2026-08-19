using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class IncomeSourceService : IIncomeSourceService
{
    private const string EntityType = "IncomeSource";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<IncomeSourceService> _logger;

    public IncomeSourceService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<IncomeSourceService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<IncomeSourceDTO> GetIncomeSources()
    {
        using var span = StartSpan("GetIncomeSources");
        try
        {
            var result = _repository.GetIncomeSources().Select(ToDto).ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetIncomeSources");
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
        _logger.LogInformation("{Operation} started", operationName);
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
