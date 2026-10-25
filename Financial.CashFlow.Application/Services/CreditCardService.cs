using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class CreditCardService : ICreditCardService
{
    private const string EntityType = "CreditCard";
    private const string DuplicateNameDisplayName = "An account";

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
            var referencedIds = AccountReferenceReader.ReferencedAccountIds(_repository);
            var statements = _repository.GetCardStatements().ToList();

            var result = _repository.GetCreditCardAccounts()
                .Select(card => ToDto(
                    card, CardDueDate.For(card, statements), latestInvoiceDateByCard.GetValueOrDefault(card.Id), referencedIds.Contains(card.Id)))
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

            _repository.GetAccounts().EnsureAccountNameIsUnique(request.Name, null, DuplicateNameDisplayName);

            var creditCard = Account.Create(request.Name, AccountTemplate.CreditCard, Currency.GBP);
            creditCard.SetActive(request.IsActive);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddAccount(creditCard);
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

            var creditCard = FindCreditCardOrThrow(id);

            _repository.GetAccounts().EnsureAccountNameIsUnique(request.Name, id, DuplicateNameDisplayName);

            await _repository.ApplyAndSaveAsync(() =>
            {
                creditCard.Update(request.Name);
                creditCard.SetActive(request.IsActive);
                SetDueDate(creditCard, request.NextInvoiceDueDate);
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
            _ = FindCreditCardOrThrow(id);

            EnsureNotReferenced(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteAccount(id);
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

    private bool IsReferenced(Guid creditCardId) => AccountReferenceReader.IsReferenced(_repository, creditCardId);

    private void SetDueDate(Account card, DateOnly? dueDate)
    {
        CardDueDate.ClearSupersededBy(card, _repository.GetCardStatements(), dueDate);

        if (dueDate is { } due)
        {
            CardStatementCreation.EnsureFor(_repository, card, due.Year, due.Month).SetDueDate(due);
        }
    }

    private Account FindCreditCardOrThrow(Guid id) =>
        _repository.GetCreditCardAccounts().FirstOrThrow(c => c.Id == id, "Credit card", id);

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

    private CreditCardDTO ToDto(Account creditCard) =>
        ToDto(creditCard, _repository.GetCardDueDate(creditCard), GetLatestInvoiceDate(creditCard.Id), IsReferenced(creditCard.Id));

    private static CreditCardDTO ToDto(Account creditCard, DateOnly? dueDate, DateOnly? latestInvoiceDate, bool hasReferences) => new()
    {
        Id = creditCard.Id,
        Name = creditCard.Name,
        IsActive = creditCard.IsActive,
        NextInvoiceDueDate = dueDate,
        LatestInvoiceDate = latestInvoiceDate,
        HasReferences = hasReferences
    };
}
