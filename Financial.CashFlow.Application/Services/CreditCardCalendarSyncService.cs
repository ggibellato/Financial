using System.Globalization;
using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

/// <summary>
/// Owns the credit-card due-date calendar-event sync rules: create/update/remove a card's
/// persistent event, self-heal when the dedicated calendar was deleted outside the app, and
/// track per-card sync status. A single <see cref="_syncLock"/> serializes every sync attempt,
/// since every one reads and rewrites the same shared <see cref="CalendarConnection"/> record
/// (event-id mapping, and the calendar id during self-healing).
/// </summary>
public sealed class CreditCardCalendarSyncService : ICreditCardCalendarSyncService
{
    private const string EntityType = "CreditCardCalendarSync";

    private readonly ICalendarProvider _provider;
    private readonly ICalendarConnectionStore _connectionStore;
    private readonly ICalendarIntegrationService _calendarIntegrationService;
    private readonly ICardStatementService _cardStatementService;
    private readonly ICashFlowRepository _repository;
    private readonly ICreditCardCalendarSyncStatusStore _statusStore;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CreditCardCalendarSyncService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public CreditCardCalendarSyncService(
        ICalendarProvider provider,
        ICalendarConnectionStore connectionStore,
        ICalendarIntegrationService calendarIntegrationService,
        ICardStatementService cardStatementService,
        ICashFlowRepository repository,
        ICreditCardCalendarSyncStatusStore statusStore,
        ITelemetryTracer tracer,
        ILogger<CreditCardCalendarSyncService> logger,
        TimeProvider timeProvider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _connectionStore = connectionStore ?? throw new ArgumentNullException(nameof(connectionStore));
        _calendarIntegrationService = calendarIntegrationService ?? throw new ArgumentNullException(nameof(calendarIntegrationService));
        _cardStatementService = cardStatementService ?? throw new ArgumentNullException(nameof(cardStatementService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _statusStore = statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void TriggerSync(Guid creditCardId)
    {
        var connection = _connectionStore.Load();
        if (connection is null || connection.RevokedReason is not null)
        {
            return;
        }

        _statusStore.SetPending(creditCardId);
        _ = Task.Run(async () =>
        {
            try
            {
                await SyncCoreAsync(creditCardId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "{Operation} background sync failed unexpectedly for card {CreditCardId}: {ExceptionType}",
                    "TriggerSync", creditCardId, ex.GetType().Name);
                _statusStore.SetError(creditCardId, "Unexpected sync failure.");
            }
        });
    }

    public async Task<CreditCardCalendarSyncStatusDTO> ResyncAsync(Guid creditCardId, CancellationToken cancellationToken = default)
    {
        if (!_repository.GetCreditCards().Any(c => c.Id == creditCardId))
        {
            throw new KeyNotFoundException($"Credit card '{creditCardId}' was not found.");
        }

        await SyncCoreAsync(creditCardId, cancellationToken).ConfigureAwait(false);
        return ToDto(creditCardId, _statusStore.GetStatus(creditCardId));
    }

    public async Task<IReadOnlyList<CreditCardCalendarSyncStatusDTO>> ResyncAllAsync(CancellationToken cancellationToken = default)
    {
        var qualifyingCardIds = QualifyingCreditCardIds();
        var results = new List<CreditCardCalendarSyncStatusDTO>();
        foreach (var cardId in qualifyingCardIds)
        {
            await SyncCoreAsync(cardId, cancellationToken).ConfigureAwait(false);
            results.Add(ToDto(cardId, _statusStore.GetStatus(cardId)));
        }

        return results;
    }

    public IReadOnlyList<CreditCardCalendarSyncStatusDTO> GetSyncStatuses() =>
        _statusStore.GetAllStatuses().Select(kvp => ToDto(kvp.Key, kvp.Value)).ToList();

    private async Task SyncCoreAsync(Guid creditCardId, CancellationToken cancellationToken)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SyncLockedAsync(creditCardId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>Assumes <see cref="_syncLock"/> is already held - never call directly except from
    /// <see cref="SyncCoreAsync"/> or from within another locked call (self-healing's per-card
    /// re-sync), or a re-entrant deadlock results.</summary>
    private async Task SyncLockedAsync(Guid creditCardId, CancellationToken cancellationToken)
    {
        using var span = StartSpan("Sync");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, creditCardId.ToString());
        try
        {
            var connection = _connectionStore.Load();
            if (connection is null || connection.RevokedReason is not null)
            {
                span.MarkSuccess();
                return;
            }

            var accessToken = await _calendarIntegrationService.GetValidAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (accessToken is null)
            {
                span.MarkSuccess();
                return;
            }

            // GetValidAccessTokenAsync may have refreshed and persisted a new token - reload.
            connection = _connectionStore.Load();
            if (connection is null || connection.RevokedReason is not null)
            {
                span.MarkSuccess();
                return;
            }

            var card = _repository.GetCreditCards().FirstOrDefault(c => c.Id == creditCardId);
            var hasEventMapping = connection.CardEventIds.TryGetValue(creditCardId, out var existingEventId);

            if (card is null || !card.IsActive || card.NextInvoiceDueDate is null)
            {
                await SyncRemovalAsync(creditCardId, connection, accessToken, hasEventMapping, existingEventId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SyncUpsertAsync(creditCardId, connection, accessToken, card.Name, card.NextInvoiceDueDate.Value, hasEventMapping, existingEventId, cancellationToken)
                    .ConfigureAwait(false);
            }

            span.MarkSuccess();
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private async Task SyncRemovalAsync(
        Guid creditCardId, CalendarConnection connection, string accessToken, bool hasEventMapping, string? existingEventId, CancellationToken cancellationToken)
    {
        if (!hasEventMapping)
        {
            _statusStore.SetSynced(creditCardId, _timeProvider.GetUtcNow());
            return;
        }

        try
        {
            await _provider.DeleteEventAsync(accessToken, connection.CalendarId, existingEventId!, cancellationToken).ConfigureAwait(false);
            RemoveEventMapping(creditCardId, connection);
            _statusStore.SetSynced(creditCardId, _timeProvider.GetUtcNow());
        }
        catch (CalendarNotFoundException)
        {
            await RecreateCalendarAndResyncAllLockedAsync(accessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to delete the event for card {CreditCardId}: {ExceptionType}", "Sync", creditCardId, ex.GetType().Name);
            _statusStore.SetError(creditCardId, ex.Message);
        }
    }

    private async Task SyncUpsertAsync(
        Guid creditCardId, CalendarConnection connection, string accessToken, string cardName, DateOnly dueDate,
        bool hasEventMapping, string? existingEventId, CancellationToken cancellationToken)
    {
        var (total, hasChargesPosted) = _cardStatementService.GetOutstandingTotalForPeriod(creditCardId, dueDate.Year, dueDate.Month);
        var title = BuildTitle(cardName, total);
        var description = BuildDescription(cardName, dueDate, total, hasChargesPosted);

        try
        {
            if (hasEventMapping)
            {
                await _provider.UpdateEventAsync(accessToken, connection.CalendarId, existingEventId!, title, description, dueDate, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var newEventId = await _provider.CreateEventAsync(accessToken, connection.CalendarId, title, description, dueDate, cancellationToken)
                    .ConfigureAwait(false);
                SetEventMapping(creditCardId, newEventId, connection);
            }

            _statusStore.SetSynced(creditCardId, _timeProvider.GetUtcNow());
        }
        catch (CalendarNotFoundException)
        {
            await RecreateCalendarAndResyncAllLockedAsync(accessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed for card {CreditCardId}: {ExceptionType}", "Sync", creditCardId, ex.GetType().Name);
            _statusStore.SetError(creditCardId, ex.Message);
        }
    }

    /// <summary>Recreates the dedicated calendar, clears the now-invalid event-id mapping, and
    /// re-syncs every qualifying card - called with <see cref="_syncLock"/> already held, so it
    /// re-syncs via <see cref="SyncLockedAsync"/> directly rather than <see cref="SyncCoreAsync"/>
    /// (which would deadlock trying to re-acquire the lock).</summary>
    private async Task RecreateCalendarAndResyncAllLockedAsync(string accessToken, CancellationToken cancellationToken)
    {
        var connection = _connectionStore.Load();
        if (connection is null || connection.RevokedReason is not null)
        {
            return;
        }

        string newCalendarId;
        try
        {
            newCalendarId = await _provider.CreateCalendarAsync(accessToken, CalendarDefaults.DedicatedCalendarName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to recreate the dedicated calendar: {ExceptionType}", "SelfHeal", ex.GetType().Name);
            return;
        }

        _connectionStore.Save(connection with { CalendarId = newCalendarId, CardEventIds = new Dictionary<Guid, string>() });

        foreach (var cardId in QualifyingCreditCardIds())
        {
            await SyncLockedAsync(cardId, cancellationToken).ConfigureAwait(false);
        }
    }

    private List<Guid> QualifyingCreditCardIds() =>
        _repository.GetCreditCards().Where(c => c.IsActive && c.NextInvoiceDueDate is not null).Select(c => c.Id).ToList();

    private void SetEventMapping(Guid creditCardId, string eventId, CalendarConnection connection)
    {
        var updated = new Dictionary<Guid, string>(connection.CardEventIds) { [creditCardId] = eventId };
        _connectionStore.Save(connection with { CardEventIds = updated });
    }

    private void RemoveEventMapping(Guid creditCardId, CalendarConnection connection)
    {
        if (!connection.CardEventIds.ContainsKey(creditCardId))
        {
            return;
        }

        var updated = new Dictionary<Guid, string>(connection.CardEventIds);
        updated.Remove(creditCardId);
        _connectionStore.Save(connection with { CardEventIds = updated });
    }

    private static string BuildTitle(string cardName, decimal total) =>
        $"{cardName} — Due {total.ToString("N2", CultureInfo.InvariantCulture)}";

    private static string BuildDescription(string cardName, DateOnly dueDate, decimal total, bool hasChargesPosted)
    {
        var formattedBalance = total.ToString("N2", CultureInfo.InvariantCulture);
        var formattedDate = dueDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var balanceLine = hasChargesPosted ? formattedBalance : $"{formattedBalance} (no charges posted to this invoice yet)";

        return string.Join('\n', cardName, $"Due {formattedDate}", balanceLine, string.Empty, "Generated automatically by the Financial app.");
    }

    private static CreditCardCalendarSyncStatusDTO ToDto(Guid creditCardId, CreditCardCalendarSyncStatus? status) => new()
    {
        CreditCardId = creditCardId,
        State = (status?.State ?? CreditCardCalendarSyncState.Pending).ToString(),
        LastSuccessfulSyncUtc = status?.LastSuccessfulSyncUtc,
        LastError = status?.LastError
    };

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CreditCardCalendarSyncService), operationName, EntityType);
    }
}
