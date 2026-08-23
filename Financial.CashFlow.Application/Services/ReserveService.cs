using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ReserveService : IReserveService
{
    private const string EntityType = "ReserveMovement";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ReserveService> _logger;

    public ReserveService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<ReserveService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IncomeSplitResultDTO> PostIncomeSplitAsync(IncomeSplitRequestDTO request)
    {
        using var span = StartSpan("PostIncomeSplit");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException("Description is required.", nameof(request.Description));
            }

            var activeBuckets = _repository.GetReserveBuckets().Where(b => b.IsActive).ToList();
            if (activeBuckets.Count == 0)
            {
                throw new ArgumentException("No reserve bucket is currently active.");
            }

            var movements = activeBuckets
                .Select(bucket => ReserveMovement.Create(bucket, bucket.CalculateSplitAmount(request.Amount), request.Date, request.Description))
                .ToList();

            try
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    foreach (var movement in movements)
                    {
                        _repository.AddReserveMovement(movement);
                    }

                    return true;
                }).ConfigureAwait(false);
            }
            catch
            {
                // The rollback edits the same graph, so it runs under the same exclusion. Reporting
                // no change is what keeps it in memory only - the failed write must not be retried.
                await _repository.ApplyAndSaveAsync(() =>
                {
                    foreach (var movement in movements)
                    {
                        _repository.DeleteReserveMovement(movement.Id);
                    }

                    return false;
                }).ConfigureAwait(false);

                throw;
            }

            var splitAmounts = movements
                .Select(movement => new BucketSplitAmountDTO
                {
                    BucketId = movement.Bucket.Id,
                    BucketName = movement.Bucket.Name,
                    Amount = movement.Amount
                })
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "PostIncomeSplit");
            return new IncomeSplitResultDTO
            {
                Buckets = splitAmounts,
                Total = splitAmounts.Sum(b => b.Amount)
            };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<ReserveMovementDTO> PostWithdrawalAsync(WithdrawalRequestDTO request)
    {
        using var span = StartSpan("PostWithdrawal");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException("Description is required.");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.");
            }

            if (!EntityIdResolver.TryResolve(request.BucketId, _repository.GetReserveBuckets(), b => b.Id, out var bucket))
            {
                throw new ArgumentException($"Reserve bucket '{request.BucketId}' is not recognized.");
            }

            var currentBalance = GetBalance(bucket!);
            if (request.Amount > currentBalance && !request.Confirmed)
            {
                throw new OverdraftConfirmationRequiredException(
                    $"This withdrawal exceeds {bucket!.Name}'s balance of {currentBalance:F2}. Set confirmed=true to proceed.");
            }

            var movement = ReserveMovement.Create(bucket!, -request.Amount, request.Date, request.Description);

            try
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    _repository.AddReserveMovement(movement);
                    return true;
                }).ConfigureAwait(false);
            }
            catch
            {
                await _repository.ApplyAndSaveAsync(() =>
                {
                    _repository.DeleteReserveMovement(movement.Id);
                    return false;
                }).ConfigureAwait(false);

                throw;
            }

            span.SetAttribute(TelemetryAttributeKeys.EntityId, movement.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "PostWithdrawal");
            return ToDto(movement);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<ReserveBucketBalanceDTO> GetBucketBalances()
    {
        using var span = StartSpan("GetBucketBalances");
        try
        {
            var balanceByBucket = _repository.GetReserveMovements()
                .GroupBy(m => m.Bucket)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Amount));

            var result = _repository.GetReserveBuckets()
                .Select(bucket => new ReserveBucketBalanceDTO
                {
                    BucketId = bucket.Id,
                    BucketName = bucket.Name,
                    Balance = balanceByBucket.GetValueOrDefault(bucket)
                })
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBucketBalances");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<ReserveMovementDTO> GetMovementHistory()
    {
        using var span = StartSpan("GetMovementHistory");
        try
        {
            var result = _repository.GetReserveMovements()
                .OrderByDescending(m => m.Date)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetMovementHistory");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<ReserveMovementDTO> UpdateMovementAsync(Guid id, UpdateReserveMovementDTO request)
    {
        using var span = StartSpan("UpdateMovement");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException("Description is required.");
            }

            if (!EntityIdResolver.TryResolve(request.BucketId, _repository.GetReserveBuckets(), b => b.Id, out var bucket))
            {
                throw new ArgumentException($"Reserve bucket '{request.BucketId}' is not recognized.");
            }

            var movement = _repository.GetReserveMovements().FirstOrThrow(m => m.Id == id, "Reserve movement", id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                movement.Update(bucket!, request.Amount, request.Date, request.Description);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateMovement");
            return ToDto(movement);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteMovementAsync(Guid id)
    {
        using var span = StartSpan("DeleteMovement");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var movement = _repository.GetReserveMovements().FirstOrThrow(m => m.Id == id, "Reserve movement", id);

            // Movements from the same income split share Date+Description (see PostIncomeSplitAsync) -
            // deleting one deletes the whole split, not just this bucket's line.
            var group = _repository.GetReserveMovements()
                .Where(m => m.Date == movement.Date && m.Description == movement.Description)
                .ToList();

            await _repository.ApplyAndSaveAsync(() =>
            {
                foreach (var groupMovement in group)
                {
                    _repository.DeleteReserveMovement(groupMovement.Id);
                }

                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteMovement");
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
        return _tracer.StartServiceSpan("CashFlow", nameof(ReserveService), operationName, EntityType);
    }

    private decimal GetBalance(ReserveBucket bucket) =>
        _repository.GetReserveMovements().Where(m => m.Bucket == bucket).Sum(m => m.Amount);

    private static ReserveMovementDTO ToDto(ReserveMovement movement) => new()
    {
        Id = movement.Id,
        BucketId = movement.Bucket.Id,
        BucketName = movement.Bucket.Name,
        Amount = movement.Amount,
        Date = movement.Date,
        Description = movement.Description
    };
}
