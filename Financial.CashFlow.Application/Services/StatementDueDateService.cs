using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class StatementDueDateService : IStatementDueDateService
{
    private const string EntityType = "CardStatement";
    private const string SetDueDateOperation = "SetDueDate";

    private readonly ICashFlowRepository _repository;
    private readonly ICreditCardCalendarSyncService _calendarSyncTrigger;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<StatementDueDateService> _logger;

    public StatementDueDateService(
        ICashFlowRepository repository,
        ICreditCardCalendarSyncService calendarSyncTrigger,
        TimeProvider timeProvider,
        ITelemetryTracer tracer,
        ILogger<StatementDueDateService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _calendarSyncTrigger = calendarSyncTrigger ?? throw new ArgumentNullException(nameof(calendarSyncTrigger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SetDueDateAsync(Guid accountId, int year, int month, StatementDueDateUpdateDTO request)
    {
        _logger.LogInformation("{Operation} started", SetDueDateOperation);
        using var span = _tracer.StartServiceSpan("CashFlow", nameof(StatementDueDateService), SetDueDateOperation, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.EntityId, accountId.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var card = FindUsableCardOrThrow(accountId);
            var listedMonth = FindListedMonthOrThrow(card, year, month);
            CardDueDate.EnsureCanChange(listedMonth, request.DueDate);

            await _repository.ApplyAndSaveAsync(() => ApplyDueDate(card, listedMonth, request.DueDate)).ConfigureAwait(false);

            _calendarSyncTrigger.TriggerSync(card.Id);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", SetDueDateOperation);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private Account FindUsableCardOrThrow(Guid accountId)
    {
        var card = _repository.GetAccounts().FirstOrThrow(account => account.Id == accountId, "Account", accountId);
        CardAccountRule.EnsureIsCard(card);
        ActiveAccountRule.EnsureUsable(null, card);
        return card;
    }

    private CardStatementLedgerMonth FindListedMonthOrThrow(Account card, int year, int month)
    {
        var today = _timeProvider.GetLocalToday();
        return CardStatementLedger
            .For(card, _repository.GetExpenses(), _repository.GetCardStatements(), _repository.GetTransfers(), today.Year, today.Month)
            .FirstOrThrow(row => row.Year == year && row.Month == month, "Card statement", $"{year}-{month:00}");
    }

    private bool ApplyDueDate(Account card, CardStatementLedgerMonth listedMonth, DateOnly? dueDate)
    {
        if (dueDate is { } date)
        {
            var statement = CardStatementCreation.EnsureFor(_repository, card, listedMonth.Year, listedMonth.Month);
            CardDueDate.SetOn(card, _repository.GetCardStatements(), statement, date);
            return true;
        }

        if (listedMonth.Statement is not { } stored)
        {
            return false;
        }

        stored.SetDueDate(null);
        return true;
    }
}
