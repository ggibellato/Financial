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
                Buckets = TransactionsOf(operation.OperationId)
                    .Select(transaction => new BucketSplitAmountDTO
                    {
                        BucketId = transaction.Pot.Id,
                        BucketName = transaction.Pot.Name,
                        Amount = transaction.Amount
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

            if (ReservaAccount.Find(_repository.GetAccounts()) is not { } potsAccount || !potsAccount.OwnsPot(request.BucketId))
            {
                throw new ArgumentException($"Reserve bucket '{request.BucketId}' is not recognized.");
            }

            var operation = await _potsService.WithdrawAsync(potsAccount.Id, new PotWithdrawalRequestDTO
            {
                PotId = request.BucketId,
                Amount = request.Amount,
                Date = request.Date,
                Description = request.Description,
                BankId = request.PaymentSourceBankId,
                CategoryId = request.ExpenseCategoryId,
                Confirmed = request.Confirmed
            }).ConfigureAwait(false);

            var transaction = TransactionsOf(operation.OperationId).Single();

            span.SetAttribute(TelemetryAttributeKeys.EntityId, transaction.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "PostWithdrawal");
            return ToDto(transaction);
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
            var transactions = _repository.GetPotTransactions().ToList();

            var result = _repository.GetReservaPots()
                .Select(pot => new ReserveBucketBalanceDTO
                {
                    BucketId = pot.Id,
                    BucketName = pot.Name,
                    Balance = AccountBalanceRule.PotBalance(pot, DateOnly.MaxValue, transactions)
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
            var result = _repository.GetPotTransactions()
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

            if (!EntityIdResolver.TryResolve(request.BucketId, _repository.GetReservaPots(), b => b.Id, out var pot))
            {
                throw new ArgumentException($"Reserve bucket '{request.BucketId}' is not recognized.");
            }

            var transaction = _repository.GetPotTransactions().FirstOrThrow(m => m.Id == id, "Reserve movement", id);
            EnsureNotLinkedToIncome(transaction);
            LinkedOperationConflictException.ThrowIfLinked(transaction.OperationId, "movement");

            await _repository.ApplyAndSaveAsync(() =>
            {
                transaction.Update(pot!, request.Amount, request.Date, request.Description);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateMovement");
            return ToDto(transaction);
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
            var transaction = _repository.GetPotTransactions().FirstOrThrow(m => m.Id == id, "Reserve movement", id);
            EnsureNotLinkedToIncome(transaction);
            LinkedOperationConflictException.ThrowIfLinked(transaction.OperationId, "movement");

            var group = _repository.GetPotTransactions()
                .Where(m => m.Date == transaction.Date && m.Description == transaction.Description && m.Income is null && m.OperationId is null)
                .ToList();

            await _repository.ApplyAndSaveAsync(() =>
            {
                foreach (var groupTransaction in group)
                {
                    _repository.DeletePotTransaction(groupTransaction.Id);
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

    private IEnumerable<PotTransaction> TransactionsOf(Guid operationId) =>
        _repository.GetPotTransactions().Where(transaction => transaction.OperationId == operationId);

    private static void EnsureNotLinkedToIncome(PotTransaction transaction)
    {
        if (transaction.Income is not null)
        {
            throw new ReserveMovementLinkedToIncomeException(
                "This reserve movement is linked to an income and can only be changed by editing that income.");
        }
    }

    private static ReserveMovementDTO ToDto(PotTransaction transaction) => new()
    {
        Id = transaction.Id,
        BucketId = transaction.Pot.Id,
        BucketName = transaction.Pot.Name,
        Amount = transaction.Amount,
        Date = transaction.Date,
        Description = transaction.Description,
        IncomeId = transaction.Income?.Id
    };
}
