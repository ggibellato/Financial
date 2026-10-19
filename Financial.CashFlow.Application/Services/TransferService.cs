using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class TransferService : ITransferService
{
    private const string EntityType = "Transfer";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TransferService> _logger;

    public TransferService(ICashFlowRepository repository, TimeProvider timeProvider, ITelemetryTracer tracer, ILogger<TransferService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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

            var transfer = Transfer.Create(
                request.Date, sourceBank, destinationBank, request.Amount, destinationAmount: null, request.Note, _timeProvider.GetLocalToday());
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddTransfer(transfer);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, transfer.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddTransfer");
            return ToDto(transfer);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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
            var destinationAmount = KeepsBothAccounts(transfer, sourceBank, destinationBank) ? transfer.DestinationAmount : null;

            await _repository.ApplyAndSaveAsync(() =>
            {
                transfer.UpdateDetails(
                    request.Date, sourceBank, destinationBank, request.Amount, destinationAmount, request.Note, _timeProvider.GetLocalToday());
                _repository.UpdateTransfer(transfer);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateTransfer");
            return ToDto(transfer);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteTransfer(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteTransfer");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTransfersByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<TransferDTO> GetTransfersByBank(Guid bankId)
    {
        using var span = StartSpan("GetTransfersByBank");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, bankId.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(bankId, _repository.GetAccounts(), b => b.Id, out var bank))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetTransfersByBank");
                return Array.Empty<TransferDTO>();
            }

            var result = _repository.GetTransfers()
                .Where(t => t.SourceAccount.Id == bank!.Id || t.DestinationAccount.Id == bank.Id)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTransfersByBank");
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
        return _tracer.StartServiceSpan("CashFlow", nameof(TransferService), operationName, EntityType);
    }

    private Transfer FindTransferOrThrow(Guid id) =>
        _repository.GetTransfers().FirstOrThrow(t => t.Id == id, "Transfer", id);

    private static bool KeepsBothAccounts(Transfer transfer, Account sourceAccount, Account destinationAccount) =>
        transfer.SourceAccount.Id == sourceAccount.Id && transfer.DestinationAccount.Id == destinationAccount.Id;

    private (Account SourceAccount, Account DestinationAccount) ResolveBanks(Guid sourceBankId, Guid destinationBankId)
    {
        var banks = _repository.GetAccounts();

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
        SourceBankId = transfer.SourceAccount.Id,
        SourceBankName = transfer.SourceAccount.Name,
        DestinationBankId = transfer.DestinationAccount.Id,
        DestinationBankName = transfer.DestinationAccount.Name,
        Amount = transfer.Amount,
        Note = transfer.Note
    };
}
