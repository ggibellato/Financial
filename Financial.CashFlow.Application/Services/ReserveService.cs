using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
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

            var transaction = _repository.GetPotTransactions().FirstOrThrow(m => m.Id == id, "Reserve movement", id);
            EnsureNotLinkedToIncome(transaction);
            LinkedOperationConflictException.ThrowIfLinked(transaction.OperationId, "movement");

            if (!EntityIdResolver.TryResolve(request.BucketId, PotsOfAccountOwning(transaction), b => b.Id, out var pot))
            {
                throw new ArgumentException($"Reserve bucket '{request.BucketId}' is not recognized.");
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                transaction.Update(pot!, request.Amount, request.Date, request.Description);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateMovement");
            return ToDto(transaction, _repository.GetPotTransactions());
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

            var lines = LinesToDeleteWith(transaction, _repository.GetPotTransactions());

            await _repository.ApplyAndSaveAsync(() =>
            {
                foreach (var line in lines)
                {
                    _repository.DeletePotTransaction(line.Id);
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

    private IEnumerable<Pot> PotsOfAccountOwning(PotTransaction transaction) =>
        _repository.GetAccounts().FirstOrDefault(account => account.OwnsPot(transaction.Pot.Id))?.Pots ?? [];

    private static IReadOnlyList<PotTransaction> LinesToDeleteWith(PotTransaction transaction, IEnumerable<PotTransaction> transactions) =>
        IsManualSplitLine(transaction)
            ? transactions.Where(line => IsLineOfSameManualSplit(line, transaction)).ToList()
            : [transaction];

    private static bool IsManualSplitLine(PotTransaction line) =>
        line.Amount > 0m
            && line.Income is null
            && line.OperationId is null;

    private static bool IsLineOfSameManualSplit(PotTransaction line, PotTransaction transaction) =>
        IsManualSplitLine(line)
            && line.Date == transaction.Date
            && line.Description == transaction.Description;

    private static void EnsureNotLinkedToIncome(PotTransaction transaction)
    {
        if (transaction.Income is not null)
        {
            throw new ReserveMovementLinkedToIncomeException(
                "This reserve movement is linked to an income and can only be changed by editing that income.");
        }
    }

    internal static ReserveMovementDTO ToDto(PotTransaction transaction, IEnumerable<PotTransaction> transactions) => new()
    {
        Id = transaction.Id,
        BucketId = transaction.Pot.Id,
        BucketName = transaction.Pot.Name,
        Amount = transaction.Amount,
        Date = transaction.Date,
        Description = transaction.Description,
        IncomeId = transaction.Income?.Id,
        SplitLineCount = LinesToDeleteWith(transaction, transactions).Count
    };
}
