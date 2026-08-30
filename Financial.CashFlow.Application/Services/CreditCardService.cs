using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
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

    public async Task<CreditCardDTO> CreateCreditCardAsync(CreditCardCreateDTO request)
    {
        using var span = StartSpan("CreateCreditCard");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Credit card name is required.", nameof(request));
            }

            EnsureNameIsUnique(request.Name, excludingId: null);

            var creditCard = CreditCard.Create(request.Name, request.IsActive);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddCreditCard(creditCard);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, creditCard.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateCreditCard");
            return ToDto(creditCard);
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

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Credit card name is required.", nameof(request));
            }

            if (!EntityIdResolver.TryResolve(id, _repository.GetCreditCards(), c => c.Id, out var creditCard))
            {
                throw new KeyNotFoundException($"Credit card '{id}' was not found.");
            }

            EnsureNameIsUnique(request.Name, excludingId: id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                creditCard!.Update(request.Name, request.IsActive, request.NextInvoiceDueDate);
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

    public async Task DeleteCreditCardAsync(Guid id)
    {
        using var span = StartSpan("DeleteCreditCard");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(id, _repository.GetCreditCards(), c => c.Id, out _))
            {
                throw new KeyNotFoundException($"Credit card '{id}' was not found.");
            }

            EnsureNotReferenced(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteCreditCard(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteCreditCard");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private void EnsureNameIsUnique(string name, Guid? excludingId)
    {
        var collision = _repository.GetCreditCards().FirstOrDefault(c => c.Name == name && c.Id != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"A credit card named \"{name}\" already exists.");
        }
    }

    private void EnsureNotReferenced(Guid creditCardId)
    {
        if (IsReferenced(creditCardId))
        {
            throw new EntityInUseException("Cannot delete a credit card that is still referenced by a statement or expense.");
        }
    }

    /// <summary>
    /// A CreditCard can be referenced directly by an Expense (before any CardStatement exists for
    /// the period) or by a CardStatement once one has been generated - both must be scanned
    /// independently. Also drives <see cref="CreditCardDTO.HasReferences"/>, so the client can
    /// disable Delete before attempting it rather than only learning about the guard from a failed
    /// request.
    /// </summary>
    private bool IsReferenced(Guid creditCardId) =>
        _repository.GetExpenses().Any(e => e.CreditCard?.Id == creditCardId) ||
        _repository.GetCardStatements().Any(s => s.CreditCard.Id == creditCardId);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CreditCardService), operationName, EntityType);
    }

    private CreditCardDTO ToDto(CreditCard creditCard) => new()
    {
        Id = creditCard.Id,
        Name = creditCard.Name,
        IsActive = creditCard.IsActive,
        NextInvoiceDueDate = creditCard.NextInvoiceDueDate,
        HasReferences = IsReferenced(creditCard.Id)
    };
}
