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
    private readonly ICreditCardCalendarSyncService _calendarSyncTrigger;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CreditCardService> _logger;

    public CreditCardService(
        ICashFlowRepository repository,
        ICreditCardCalendarSyncService calendarSyncTrigger,
        ITelemetryTracer tracer,
        ILogger<CreditCardService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _calendarSyncTrigger = calendarSyncTrigger ?? throw new ArgumentNullException(nameof(calendarSyncTrigger));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<CreditCardDTO> GetCreditCards()
    {
        using var span = StartSpan("GetCreditCards");
        try
        {
            var latestInvoiceDateByCard = _repository.GetExpenses()
                .Where(e => e.CreditCard is not null)
                .GroupBy(e => e.CreditCard!.Id)
                .ToDictionary(g => g.Key, g => g.Max(e => e.InvoiceDate));
            var cardIdsWithStatements = _repository.GetCardStatements().Select(s => s.CreditCard.Id).ToHashSet();

            var result = _repository.GetCreditCards()
                .Select(card => ToDto(
                    card,
                    latestInvoiceDateByCard.GetValueOrDefault(card.Id),
                    latestInvoiceDateByCard.ContainsKey(card.Id) || cardIdsWithStatements.Contains(card.Id)))
                .ToList();

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

            _repository.GetCreditCards().EnsureNameIsUnique(request.Name, null, c => c.Name, c => c.Id, "A credit card");

            var creditCard = CreditCard.Create(request.Name, request.IsActive);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddCreditCard(creditCard);
                return true;
            }).ConfigureAwait(false);

            // Fire-and-forget: never affects this save's own success (see CreditCardCalendarSyncService).
            _calendarSyncTrigger.TriggerSync(creditCard.Id);

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

            _repository.GetCreditCards().EnsureNameIsUnique(request.Name, id, c => c.Name, c => c.Id, "A credit card");

            await _repository.ApplyAndSaveAsync(() =>
            {
                creditCard!.Update(request.Name, request.IsActive, request.NextInvoiceDueDate);
                return true;
            }).ConfigureAwait(false);

            _calendarSyncTrigger.TriggerSync(id);

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

            // The card is already gone from the repository; the sync service treats a
            // no-longer-existing id the same as inactive/no-due-date - it removes the event.
            _calendarSyncTrigger.TriggerSync(id);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteCreditCard");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
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

    private DateOnly? GetLatestInvoiceDate(Guid creditCardId) =>
        _repository.GetExpenses()
            .Where(e => e.CreditCard?.Id == creditCardId)
            .Select(e => e.InvoiceDate)
            .Max();

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CreditCardService), operationName, EntityType);
    }

    private CreditCardDTO ToDto(CreditCard creditCard) =>
        ToDto(creditCard, GetLatestInvoiceDate(creditCard.Id), IsReferenced(creditCard.Id));

    private static CreditCardDTO ToDto(CreditCard creditCard, DateOnly? latestInvoiceDate, bool hasReferences) => new()
    {
        Id = creditCard.Id,
        Name = creditCard.Name,
        IsActive = creditCard.IsActive,
        NextInvoiceDueDate = creditCard.NextInvoiceDueDate,
        LatestInvoiceDate = latestInvoiceDate,
        HasReferences = hasReferences
    };
}
