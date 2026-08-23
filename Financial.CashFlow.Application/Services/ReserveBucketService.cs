using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ReserveBucketService : IReserveBucketService
{
    private const string EntityType = "ReserveBucket";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ReserveBucketService> _logger;

    public ReserveBucketService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<ReserveBucketService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<ReserveBucketDTO> GetReserveBuckets()
    {
        using var span = StartSpan("GetReserveBuckets");
        try
        {
            var result = _repository.GetReserveBuckets().Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetReserveBuckets");
            return result;
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
        return _tracer.StartServiceSpan("CashFlow", nameof(ReserveBucketService), operationName, EntityType);
    }

    private static ReserveBucketDTO ToDto(ReserveBucket bucket) => new()
    {
        Id = bucket.Id,
        Name = bucket.Name,
        IsActive = bucket.IsActive,
        SplitPercentage = bucket.SplitPercentage
    };
}
