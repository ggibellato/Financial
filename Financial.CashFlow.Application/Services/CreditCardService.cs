using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class CreditCardService : ICreditCardService
{
    private const string EntityType = "CreditCard";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CreditCardService> _logger;

    public CreditCardService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<CreditCardService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<CreditCardDTO> GetCreditCards()
    {
        using var span = StartSpan("GetCreditCards");
        try
        {
            var result = _repository.GetCreditCards().Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCreditCards");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request)
    {
        using var span = StartSpan("UpdateCreditCard");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!EntityIdResolver.TryResolve(id, _repository.GetCreditCards(), c => c.Id, out var creditCard))
            {
                throw new KeyNotFoundException($"Credit card '{id}' was not found.");
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                creditCard!.UpdateDetails(request.NextInvoiceDueDate, request.IsActive);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateCreditCard");
            return ToDto(creditCard);
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
        return _tracer.StartServiceSpan("CashFlow", nameof(CreditCardService), operationName, EntityType);
    }

    private static CreditCardDTO ToDto(CreditCard creditCard) => new()
    {
        Id = creditCard.Id,
        Name = creditCard.Name,
        IsActive = creditCard.IsActive,
        NextInvoiceDueDate = creditCard.NextInvoiceDueDate
    };
}
