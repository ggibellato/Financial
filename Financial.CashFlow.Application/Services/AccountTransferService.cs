using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class AccountTransferService : IAccountTransferService
{
    private const string EntityType = "Transfer";
    private const string AccountEntityType = "Account";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AccountTransferService> _logger;

    public AccountTransferService(
        ICashFlowRepository repository, TimeProvider timeProvider, ITelemetryTracer tracer, ILogger<AccountTransferService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AccountTransferDTO> AddAsync(AccountTransferCreateDTO request)
    {
        using var span = StartSpan("AddTransfer");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var (source, destination) = ResolveAccounts(request.SourceAccountId, request.DestinationAccountId);
            var transfer = Transfer.Create(
                request.Date, source, destination, request.Amount, request.DestinationAmount, request.Note, _timeProvider.GetLocalToday());

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

    public async Task<AccountTransferDTO> UpdateAsync(Guid id, AccountTransferUpdateDTO request)
    {
        using var span = StartSpan("UpdateTransfer");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var transfer = FindEditableTransferOrThrow(id);
            var (source, destination) = ResolveAccounts(request.SourceAccountId, request.DestinationAccountId);

            await _repository.ApplyAndSaveAsync(() =>
            {
                transfer.UpdateDetails(
                    request.Date, source, destination, request.Amount, request.DestinationAmount, request.Note, _timeProvider.GetLocalToday());
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

    public async Task DeleteAsync(Guid id)
    {
        using var span = StartSpan("DeleteTransfer");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            FindEditableTransferOrThrow(id);

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

    public IReadOnlyList<TransferAccountOptionDTO> GetDestinations(Guid accountId) =>
        ListCounterparts("GetTransferDestinations", accountId, TransferPairRules.Default.DestinationsFor);

    public IReadOnlyList<TransferAccountOptionDTO> GetSources(Guid accountId) =>
        ListCounterparts("GetTransferSources", accountId, TransferPairRules.Default.SourcesFor);

    private IReadOnlyList<TransferAccountOptionDTO> ListCounterparts(
        string operationName, Guid accountId, Func<Account, IEnumerable<Account>, IReadOnlyList<Account>> counterparts)
    {
        using var span = StartSpan(operationName);
        span.SetAttribute(TelemetryAttributeKeys.EntityId, accountId.ToString());
        try
        {
            var accounts = _repository.GetAccounts().ToList();
            var account = accounts.FirstOrThrow(a => a.Id == accountId, AccountEntityType, accountId);
            var result = counterparts(account, accounts).Select(ToOptionDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", operationName);
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
        return _tracer.StartServiceSpan("CashFlow", nameof(AccountTransferService), operationName, EntityType);
    }

    private Transfer FindEditableTransferOrThrow(Guid id)
    {
        var transfer = _repository.GetTransfers().FirstOrThrow(t => t.Id == id, EntityType, id);
        StatementPaymentConflictException.ThrowIfStatementPayment(transfer);
        return transfer;
    }

    private (Account Source, Account Destination) ResolveAccounts(Guid sourceAccountId, Guid destinationAccountId)
    {
        var accounts = _repository.GetAccounts();
        return (
            CashFlowReferenceResolver.ResolveAccount(accounts, sourceAccountId),
            CashFlowReferenceResolver.ResolveAccount(accounts, destinationAccountId));
    }

    internal static AccountTransferDTO ToDto(Transfer transfer) => new()
    {
        Id = transfer.Id,
        Date = transfer.Date,
        SourceAccountId = transfer.SourceAccount.Id,
        SourceAccountName = transfer.SourceAccount.Name,
        SourceCurrency = transfer.SourceAccount.Currency.ToString(),
        DestinationAccountId = transfer.DestinationAccount.Id,
        DestinationAccountName = transfer.DestinationAccount.Name,
        DestinationCurrency = transfer.DestinationAccount.Currency.ToString(),
        Amount = transfer.Amount,
        DestinationAmount = transfer.AmountReceived(),
        Note = transfer.Note
    };

    private static TransferAccountOptionDTO ToOptionDto(Account account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Currency = account.Currency.ToString()
    };
}
