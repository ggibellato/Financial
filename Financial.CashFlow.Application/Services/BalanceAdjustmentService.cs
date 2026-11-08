using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class BalanceAdjustmentService : IBalanceAdjustmentService
{
    private const string EntityType = "BalanceAdjustment";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<BalanceAdjustmentService> _logger;

    public BalanceAdjustmentService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<BalanceAdjustmentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BalanceAdjustmentDTO> AddAdjustmentAsync(Guid accountId, BalanceAdjustmentCreateDTO request)
    {
        using var span = StartSpan("AddAdjustment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, accountId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = FindAccountOrThrow(accountId);
            var currentBalance = AccountBalanceRule.Balance(account, request.Date, AccountTransactionReader.Read(_repository));
            var delta = request.TargetBalance - currentBalance;

            var adjustment = BalanceAdjustment.Create(request.Date, account, request.TargetBalance, delta, request.Note);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddBalanceAdjustment(adjustment);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddAdjustment");
            return ToDto(adjustment);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(Guid accountId, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        using var span = StartSpan("UpdateAdjustment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = FindAccountOrThrow(accountId);
            var adjustment = FindAdjustmentOrThrow(account, id);
            var otherTransactions = AccountTransactionReader.Read(_repository).Where(transaction => !ReferenceEquals(transaction, adjustment));
            var currentBalance = AccountBalanceRule.Balance(account, request.Date, otherTransactions);
            var delta = request.TargetBalance - currentBalance;

            await _repository.ApplyAndSaveAsync(() =>
            {
                adjustment.UpdateDetails(request.Date, request.TargetBalance, delta, request.Note);
                _repository.UpdateBalanceAdjustment(adjustment);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateAdjustment");
            return ToDto(adjustment);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteAdjustmentAsync(Guid accountId, Guid id)
    {
        using var span = StartSpan("DeleteAdjustment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            FindAdjustmentOrThrow(FindAccountOrThrow(accountId), id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteBalanceAdjustment(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteAdjustment");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(Guid bankId)
    {
        using var span = StartSpan("GetAdjustmentsByBank");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, bankId.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetAccounts(), b => b.Id, out var bank))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetAdjustmentsByBank");
                return Array.Empty<BalanceAdjustmentDTO>();
            }

            var result = _repository.GetBalanceAdjustments()
                .Where(a => a.Account.Id == bank!.Id)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAdjustmentsByBank");
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
        return _tracer.StartServiceSpan("CashFlow", nameof(BalanceAdjustmentService), operationName, EntityType);
    }

    private Account FindAccountOrThrow(Guid accountId) =>
        _repository.GetAccounts().FirstOrThrow(account => account.Id == accountId, "Account", accountId);

    private BalanceAdjustment FindAdjustmentOrThrow(Account account, Guid id) =>
        _repository.GetBalanceAdjustments()
            .FirstOrThrow(a => a.Id == id && a.Account.Id == account.Id, "Balance adjustment", id);

    internal static BalanceAdjustmentDTO ToDto(BalanceAdjustment adjustment) => new()
    {
        Id = adjustment.Id,
        Date = adjustment.Date,
        BankId = adjustment.Account.Id,
        BankName = adjustment.Account.Name,
        TargetBalance = adjustment.TargetBalance,
        Delta = adjustment.Delta,
        Note = adjustment.Note
    };
}
