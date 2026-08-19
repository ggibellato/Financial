using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class BalanceAdjustmentService : IBalanceAdjustmentService
{
    private const string EntityType = "BalanceAdjustment";

    private readonly ICashFlowRepository _repository;
    private readonly IBankService _bankService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<BalanceAdjustmentService> _logger;

    public BalanceAdjustmentService(ICashFlowRepository repository, IBankService bankService, ITelemetryTracer tracer, ILogger<BalanceAdjustmentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BalanceAdjustmentDTO> AddAdjustmentAsync(Guid bankId, BalanceAdjustmentCreateDTO request)
    {
        using var span = StartSpan("AddAdjustment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, bankId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var bank = ResolveBank(bankId);
            var currentBalance = _bankService.GetBankBalanceAsOf(bank.Id, request.Date);
            var delta = request.TargetBalance - currentBalance;

            var adjustment = BalanceAdjustment.Create(request.Date, bank, request.TargetBalance, delta, request.Note);
            _repository.AddBalanceAdjustment(adjustment);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "AddAdjustment");
            return ToDto(adjustment);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<BalanceAdjustmentDTO> UpdateAdjustmentAsync(Guid bankId, Guid id, BalanceAdjustmentUpdateDTO request)
    {
        using var span = StartSpan("UpdateAdjustment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var bank = ResolveBank(bankId);
            var adjustment = FindAdjustmentOrThrow(bank, id);
            var currentBalance = _bankService.GetBankBalanceAsOf(bank.Id, request.Date, excludingAdjustmentId: id);
            var delta = request.TargetBalance - currentBalance;

            adjustment.UpdateDetails(request.Date, request.TargetBalance, delta, request.Note);
            _repository.UpdateBalanceAdjustment(adjustment);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "UpdateAdjustment");
            return ToDto(adjustment);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task DeleteAdjustmentAsync(Guid bankId, Guid id)
    {
        using var span = StartSpan("DeleteAdjustment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var bank = ResolveBank(bankId);
            FindAdjustmentOrThrow(bank, id);

            _repository.DeleteBalanceAdjustment(id);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "DeleteAdjustment");
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IReadOnlyList<BalanceAdjustmentDTO> GetAdjustmentsByBank(Guid bankId)
    {
        using var span = StartSpan("GetAdjustmentsByBank");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, bankId.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetAdjustmentsByBank");
                return Array.Empty<BalanceAdjustmentDTO>();
            }

            var result = _repository.GetBalanceAdjustments()
                .Where(a => a.Bank.Id == bank!.Id)
                .Select(ToDto)
                .ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetAdjustmentsByBank");
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
        var span = _tracer.StartSpan($"CashFlow.BalanceAdjustmentService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private Bank ResolveBank(Guid bankId)
    {
        if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
        {
            throw new ArgumentException($"Bank '{bankId}' was not found.");
        }

        return bank!;
    }

    private BalanceAdjustment FindAdjustmentOrThrow(Bank bank, Guid id) =>
        _repository.GetBalanceAdjustments()
            .FirstOrThrow(a => a.Id == id && a.Bank.Id == bank.Id, "Balance adjustment", id);

    private static BalanceAdjustmentDTO ToDto(BalanceAdjustment adjustment) => new()
    {
        Id = adjustment.Id,
        Date = adjustment.Date,
        BankId = adjustment.Bank.Id,
        BankName = adjustment.Bank.Name,
        TargetBalance = adjustment.TargetBalance,
        Delta = adjustment.Delta,
        Note = adjustment.Note
    };
}
