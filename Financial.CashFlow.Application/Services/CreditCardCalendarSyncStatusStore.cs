using System.Collections.Concurrent;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Application.Services;

public sealed class CreditCardCalendarSyncStatusStore : ICreditCardCalendarSyncStatusStore
{
    private readonly ConcurrentDictionary<Guid, CreditCardCalendarSyncStatus> _statuses = new();

    public void SetPending(Guid creditCardId) =>
        _statuses[creditCardId] = new CreditCardCalendarSyncStatus(CreditCardCalendarSyncState.Pending, LastSuccessfulSyncUtc(creditCardId), null);

    public void SetSynced(Guid creditCardId, DateTimeOffset syncedAtUtc) =>
        _statuses[creditCardId] = new CreditCardCalendarSyncStatus(CreditCardCalendarSyncState.Synced, syncedAtUtc, null);

    public void SetError(Guid creditCardId, string error) =>
        _statuses[creditCardId] = new CreditCardCalendarSyncStatus(CreditCardCalendarSyncState.Error, LastSuccessfulSyncUtc(creditCardId), error);

    public void Clear(Guid creditCardId) => _statuses.TryRemove(creditCardId, out _);

    public CreditCardCalendarSyncStatus? GetStatus(Guid creditCardId) =>
        _statuses.TryGetValue(creditCardId, out var status) ? status : null;

    public IReadOnlyDictionary<Guid, CreditCardCalendarSyncStatus> GetAllStatuses() =>
        new Dictionary<Guid, CreditCardCalendarSyncStatus>(_statuses);

    private DateTimeOffset? LastSuccessfulSyncUtc(Guid creditCardId) =>
        _statuses.TryGetValue(creditCardId, out var status) ? status.LastSuccessfulSyncUtc : null;
}
