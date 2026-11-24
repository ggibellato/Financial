using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Validation;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ExternalPaymentService : IExternalPaymentService
{
    private const string EntityType = "ExternalPayment";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ExternalPaymentService> _logger;

    public ExternalPaymentService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<ExternalPaymentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExternalPaymentDTO> AddPaymentAsync(Guid accountId, ExternalPaymentCreateDTO request)
    {
        using var span = StartSpan("AddPayment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, accountId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var payment = ExternalPayment.Create(
                ParseDirection(request.Direction), request.Date, FindAccountOrThrow(accountId), request.Amount, request.Description);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddExternalPayment(payment);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddPayment");
            return ToDto(payment);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<ExternalPaymentDTO> UpdatePaymentAsync(Guid accountId, Guid id, ExternalPaymentUpdateDTO request)
    {
        using var span = StartSpan("UpdatePayment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var payment = FindPaymentOrThrow(FindAccountOrThrow(accountId), id);
            await _repository.ApplyAndSaveAsync(() =>
            {
                payment.UpdateDetails(request.Date, request.Amount, request.Description);
                _repository.UpdateExternalPayment(payment);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdatePayment");
            return ToDto(payment);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeletePaymentAsync(Guid accountId, Guid id)
    {
        using var span = StartSpan("DeletePayment");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            FindPaymentOrThrow(FindAccountOrThrow(accountId), id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteExternalPayment(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeletePayment");
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
        return _tracer.StartServiceSpan("CashFlow", nameof(ExternalPaymentService), operationName, EntityType);
    }

    private static PaymentDirection ParseDirection(string direction) =>
        EnumParser.TryParseEnum<PaymentDirection>(direction, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(direction, out _)
            ? parsed
            : throw new ArgumentException("Choose whether money is paid in or out.");

    private Account FindAccountOrThrow(Guid accountId) =>
        _repository.GetAccounts().FirstOrThrow(account => account.Id == accountId, "Account", accountId);

    private ExternalPayment FindPaymentOrThrow(Account account, Guid id) =>
        _repository.GetExternalPayments().FirstOrThrow(payment => payment.Id == id && payment.Account.Id == account.Id, "Payment", id);

    internal static ExternalPaymentDTO ToDto(ExternalPayment payment) => new()
    {
        Id = payment.Id,
        Direction = payment.Direction.ToString(),
        Date = payment.Date,
        AccountId = payment.Account.Id,
        AccountName = payment.Account.Name,
        Amount = payment.Amount,
        Description = payment.Description
    };
}
