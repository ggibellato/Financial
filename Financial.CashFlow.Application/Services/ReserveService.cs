using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ReserveService : IReserveService
{
    private const string EntityType = "ReserveMovement";

    private readonly ICashFlowRepository _repository;
    private readonly IPotsService _potsService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ReserveService> _logger;

    public ReserveService(ICashFlowRepository repository, IPotsService potsService, ITelemetryTracer tracer, ILogger<ReserveService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _potsService = potsService ?? throw new ArgumentNullException(nameof(potsService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IncomeSplitResultDTO> PostIncomeSplitAsync(IncomeSplitRequestDTO request)
    {
        using var span = StartSpan("PostIncomeSplit");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var potsAccount = ReservaAccount.Find(_repository.GetAccounts())
                ?? throw new ArgumentException("No reserve bucket is currently active.");

            var operation = await _potsService.DepositAsync(potsAccount.Id, new PotDepositRequestDTO
            {
                Amount = request.Amount,
                Date = request.Date,
                Description = request.Description
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "PostIncomeSplit");
            return new IncomeSplitResultDTO
            {
                Buckets = MovementsOf(operation.OperationId)
                    .Select(movement => new BucketSplitAmountDTO
                    {
                        BucketId = movement.Bucket.Id,
                        BucketName = movement.Bucket.Name,
                        Amount = movement.Amount
                    })
                    .ToList(),
                Total = operation.Amount
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

            if (!EntityIdResolver.TryResolve(request.BucketId, _repository.GetReserveBuckets(), b => b.Id, out _))
            {
                throw new ArgumentException($"Reserve bucket '{request.BucketId}' is not recognized.");
            }

            var operation = await _potsService.WithdrawAsync(ReservaAccount.Find(_repository.GetAccounts())!.Id, new PotWithdrawalRequestDTO
            {
                PotId = request.BucketId,
                Amount = request.Amount,
                Date = request.Date,
                Description = request.Description,
                BankId = request.PaymentSourceBankId,
                CategoryId = request.ExpenseCategoryId,
                Confirmed = request.Confirmed
            }).ConfigureAwait(false);

            var movement = MovementsOf(operation.OperationId).Single();

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
            var movements = _repository.GetReserveMovements().ToList();

            var result = _repository.GetReserveBuckets()
                .Select(bucket => new ReserveBucketBalanceDTO
                {
                    BucketId = bucket.Id,
                    BucketName = bucket.Name,
                    Balance = AccountBalanceRule.PotBalance(bucket, DateOnly.MaxValue, movements)
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

    public async Task<ReserveMovementDTO> UpdateMovementAsync(Guid id, ReserveMovementUpdateDTO request)
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
            EnsureNotLinkedToIncome(movement);
            LinkedOperationConflictException.ThrowIfLinked(movement.OperationId, "movement");

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
            EnsureNotLinkedToIncome(movement);
            LinkedOperationConflictException.ThrowIfLinked(movement.OperationId, "movement");

            var group = _repository.GetReserveMovements()
                .Where(m => m.Date == movement.Date && m.Description == movement.Description && m.Income is null && m.OperationId is null)
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

    internal static List<ReserveMovement> CreateSplitMovements(
        IEnumerable<ReserveBucket> activeBuckets, decimal amount, DateOnly date, string description, Income? income = null)
    {
        var buckets = activeBuckets.ToList();
        var amounts = ReserveSplitAllocator.Allocate(buckets, amount);

        return buckets
            .Select((bucket, i) => ReserveMovement.Create(bucket, amounts[i], date, description, income))
            .ToList();
    }

    private IEnumerable<ReserveMovement> MovementsOf(Guid operationId) =>
        _repository.GetReserveMovements().Where(movement => movement.OperationId == operationId);

    private static void EnsureNotLinkedToIncome(ReserveMovement movement)
    {
        if (movement.Income is not null)
        {
            throw new ReserveMovementLinkedToIncomeException(
                "This reserve movement is linked to an income and can only be changed by editing that income.");
        }
    }

    private static ReserveMovementDTO ToDto(ReserveMovement movement) => new()
    {
        Id = movement.Id,
        BucketId = movement.Bucket.Id,
        BucketName = movement.Bucket.Name,
        Amount = movement.Amount,
        Date = movement.Date,
        Description = movement.Description,
        IncomeId = movement.Income?.Id
    };
}
