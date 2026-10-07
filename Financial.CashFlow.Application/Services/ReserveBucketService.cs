using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
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
            var result = _repository.GetReserveBuckets().Select(b => ToDto(b, warning: null)).ToList();

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
            var buckets = _repository.GetReserveBuckets().ToList();
            var total = ActiveSplitTotal(buckets);
            var result = new ReserveSplitStatusDTO
            {
                ActiveTotal = total,
                Warning = buckets.Count == 0 ? null : WarningFor(total),
            };

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

    public async Task<ReserveBucketDTO> CreateReserveBucketAsync(ReserveBucketCreateDTO request)
    {
        using var span = StartSpan("CreateReserveBucket");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Reserve bucket name is required.", nameof(request));
            }

            _repository.GetReserveBuckets().EnsureNameIsUnique(request.Name, null, b => b.Name, b => b.Id, "A reserve bucket");

            var bucket = ReserveBucket.Create(request.Name, request.SplitPercentage, request.IsActive);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddReserveBucket(bucket);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, bucket.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateReserveBucket");
            return ToDto(bucket, ComputeActiveSplitWarning());
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<ReserveBucketDTO> UpdateReserveBucketAsync(Guid id, ReserveBucketUpdateDTO request)
    {
        using var span = StartSpan("UpdateReserveBucket");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Reserve bucket name is required.", nameof(request));
            }

            if (!EntityIdResolver.TryResolve(id, _repository.GetReserveBuckets(), b => b.Id, out var bucket))
            {
                throw new KeyNotFoundException($"Reserve bucket '{id}' was not found.");
            }

            _repository.GetReserveBuckets().EnsureNameIsUnique(request.Name, id, b => b.Name, b => b.Id, "A reserve bucket");

            await _repository.ApplyAndSaveAsync(() =>
            {
                bucket!.Update(request.Name, request.SplitPercentage, request.IsActive);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateReserveBucket");
            return ToDto(bucket, ComputeActiveSplitWarning());
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static decimal ActiveSplitTotal(IEnumerable<ReserveBucket> buckets) => buckets.Where(b => b.IsActive).Sum(b => b.SplitPercentage);

    private static string? WarningFor(decimal total) => ReserveSplitRule.IsBalanced(total) ? null : ReserveSplitRule.BuildWarning(total);

    private string? ComputeActiveSplitWarning() => WarningFor(ActiveSplitTotal(_repository.GetReserveBuckets()));

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(ReserveBucketService), operationName, EntityType);
    }

    private static ReserveBucketDTO ToDto(ReserveBucket bucket, string? warning) => new()
    {
        Id = bucket.Id,
        Name = bucket.Name,
        IsActive = bucket.IsActive,
        SplitPercentage = bucket.SplitPercentage,
        Warning = warning
    };
}
