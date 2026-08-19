using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class TransferService : ITransferService
{
    private const string EntityType = "Transfer";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TransferService> _logger;

    public TransferService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<TransferService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TransferDTO> AddTransferAsync(TransferCreateDTO request)
    {
        using var span = StartSpan("AddTransfer");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var (sourceBank, destinationBank) = ResolveBanks(request.SourceBankId, request.DestinationBankId);

            var transfer = Transfer.Create(request.Date, sourceBank, destinationBank, request.Amount, request.Note);
            _repository.AddTransfer(transfer);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, transfer.Id.ToString());
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "AddTransfer");
            return ToDto(transfer);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<TransferDTO> UpdateTransferAsync(Guid id, TransferUpdateDTO request)
    {
        using var span = StartSpan("UpdateTransfer");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var transfer = FindTransferOrThrow(id);
            var (sourceBank, destinationBank) = ResolveBanks(request.SourceBankId, request.DestinationBankId);

            transfer.UpdateDetails(request.Date, sourceBank, destinationBank, request.Amount, request.Note);
            _repository.UpdateTransfer(transfer);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "UpdateTransfer");
            return ToDto(transfer);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task DeleteTransferAsync(Guid id)
    {
        using var span = StartSpan("DeleteTransfer");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            FindTransferOrThrow(id);

            _repository.DeleteTransfer(id);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "DeleteTransfer");
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IReadOnlyList<TransferDTO> GetTransfersByMonth(int year, int month)
    {
        using var span = StartSpan("GetTransfersByMonth");
        try
        {
            var result = _repository.GetTransfers()
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .Select(ToDto)
                .ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetTransfersByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IReadOnlyList<TransferDTO> GetTransfersByBank(Guid bankId)
    {
        using var span = StartSpan("GetTransfersByBank");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, bankId.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetBanks(), b => b.Id, out var bank))
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetTransfersByBank");
                return Array.Empty<TransferDTO>();
            }

            var result = _repository.GetTransfers()
                .Where(t => t.SourceBank.Id == bank!.Id || t.DestinationBank.Id == bank.Id)
                .Select(ToDto)
                .ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetTransfersByBank");
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
        var span = _tracer.StartSpan($"CashFlow.TransferService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private Transfer FindTransferOrThrow(Guid id) =>
        _repository.GetTransfers().FirstOrThrow(t => t.Id == id, "Transfer", id);

    private (Bank SourceBank, Bank DestinationBank) ResolveBanks(Guid sourceBankId, Guid destinationBankId)
    {
        var banks = _repository.GetBanks();

        if (!EntityIdResolver.TryResolve(sourceBankId, banks, b => b.Id, out var resolvedSource))
        {
            throw new ArgumentException($"Bank '{sourceBankId}' was not found.");
        }

        if (!EntityIdResolver.TryResolve(destinationBankId, banks, b => b.Id, out var resolvedDestination))
        {
            throw new ArgumentException($"Bank '{destinationBankId}' was not found.");
        }

        return (resolvedSource!, resolvedDestination!);
    }

    private static TransferDTO ToDto(Transfer transfer) => new()
    {
        Id = transfer.Id,
        Date = transfer.Date,
        SourceBankId = transfer.SourceBank.Id,
        SourceBankName = transfer.SourceBank.Name,
        DestinationBankId = transfer.DestinationBank.Id,
        DestinationBankName = transfer.DestinationBank.Name,
        Amount = transfer.Amount,
        Note = transfer.Note
    };
}
