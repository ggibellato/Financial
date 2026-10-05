using System.Collections.Concurrent;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.CashFlow.Application.Services;

namespace Financial.CashFlow.Application.Tests.Services;

internal sealed class SignallingCalendarSyncStatusStore : ICreditCardCalendarSyncStatusStore
{
    private static readonly TimeSpan GuardTimeout = TimeSpan.FromSeconds(30);

    private readonly CreditCardCalendarSyncStatusStore _inner = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<CreditCardCalendarSyncState>> _resolutions = new();

    public Task<CreditCardCalendarSyncState> ResolvedStateAsync(Guid creditCardId) =>
        Resolution(creditCardId).Task.WaitAsync(GuardTimeout);

    public void SetPending(Guid creditCardId)
    {
        _resolutions[creditCardId] = NewResolution();
        _inner.SetPending(creditCardId);
    }

    public void SetSynced(Guid creditCardId, DateTimeOffset syncedAtUtc)
    {
        _inner.SetSynced(creditCardId, syncedAtUtc);
        Resolution(creditCardId).TrySetResult(CreditCardCalendarSyncState.Synced);
    }

    public void SetError(Guid creditCardId, string error)
    {
        _inner.SetError(creditCardId, error);
        Resolution(creditCardId).TrySetResult(CreditCardCalendarSyncState.Error);
    }

    public void Clear(Guid creditCardId) => _inner.Clear(creditCardId);

    public CreditCardCalendarSyncStatus? GetStatus(Guid creditCardId) => _inner.GetStatus(creditCardId);

    public IReadOnlyDictionary<Guid, CreditCardCalendarSyncStatus> GetAllStatuses() => _inner.GetAllStatuses();

    private TaskCompletionSource<CreditCardCalendarSyncState> Resolution(Guid creditCardId) =>
        _resolutions.GetOrAdd(creditCardId, _ => NewResolution());

    private static TaskCompletionSource<CreditCardCalendarSyncState> NewResolution() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
