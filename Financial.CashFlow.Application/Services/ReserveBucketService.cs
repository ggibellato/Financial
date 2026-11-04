using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
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
            var result = _repository.GetReservaPots().Select(b => ToDto(b, warning: null)).ToList();

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

    public ReserveSplitStatusDTO GetSplitStatus()
    {
        using var span = StartSpan("GetSplitStatus");
        try
        {
            var result = BuildSplitStatus();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetSplitStatus");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ReserveSplitStatusDTO BuildSplitStatus()
    {
        var pots = _repository.GetReservaPots();
        var total = pots.Where(b => b.IsActive).Sum(b => b.SplitPercentage);
        return new ReserveSplitStatusDTO
        {
            ActiveTotal = total,
            Warning = !pots.Any() || ReserveSplitRule.IsBalanced(total) ? null : ReserveSplitRule.BuildWarning(total),
        };
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(ReserveBucketService), operationName, EntityType);
    }

    private static ReserveBucketDTO ToDto(Pot pot, string? warning) => new()
    {
        Id = pot.Id,
        Name = pot.Name,
        IsActive = pot.IsActive,
        SplitPercentage = pot.SplitPercentage,
        Warning = warning
    };
}
