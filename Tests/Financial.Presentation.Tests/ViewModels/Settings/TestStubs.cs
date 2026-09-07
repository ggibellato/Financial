using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;

namespace Financial.Presentation.Tests.ViewModels.Settings;

internal sealed class StubCalendarIntegrationService : ICalendarIntegrationService
{
    public string AuthorizationUrl { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth?state=fake";
    public CalendarConnectionStatusDTO StatusToReturn { get; set; } = new() { Connected = false };
    public CalendarDisconnectResultDTO DisconnectResult { get; set; } = new() { RemoteCleanupSucceeded = true };
    public Exception? ThrowOnGetStatus { get; set; }
    public Exception? ThrowOnDisconnect { get; set; }
    public int DisconnectCallCount { get; private set; }
    public int GetStatusCallCount { get; private set; }

    public string BuildAuthorizationUrl() => AuthorizationUrl;

    public Task<CalendarCallbackResultDTO> CompleteConnectionAsync(
        string? code, string? state, string? error, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CalendarConnectionStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        GetStatusCallCount++;
        return ThrowOnGetStatus is null ? Task.FromResult(StatusToReturn) : throw ThrowOnGetStatus;
    }

    public Task<CalendarDisconnectResultDTO> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisconnectCallCount++;
        return ThrowOnDisconnect is null ? Task.FromResult(DisconnectResult) : throw ThrowOnDisconnect;
    }

    public Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class StubCreditCardCalendarSyncService : ICreditCardCalendarSyncService
{
    public List<CreditCardCalendarSyncStatusDTO> Statuses { get; set; } = [];
    public CreditCardCalendarSyncStatusDTO? ResyncResult { get; set; }
    public Exception? ThrowOnResync { get; set; }
    public Guid? LastResyncedCardId { get; private set; }

    /// <summary>When set, ResyncAsync returns this uncompleted task instead of resolving
    /// immediately, so a test can observe state while the resync is still in flight.</summary>
    public TaskCompletionSource<CreditCardCalendarSyncStatusDTO>? PendingResync { get; set; }

    public void TriggerSync(Guid creditCardId)
    {
    }

    public Task<CreditCardCalendarSyncStatusDTO> ResyncAsync(Guid creditCardId, CancellationToken cancellationToken = default)
    {
        LastResyncedCardId = creditCardId;
        if (ThrowOnResync is not null)
        {
            throw ThrowOnResync;
        }

        if (PendingResync is not null)
        {
            return PendingResync.Task;
        }

        return Task.FromResult(ResyncResult ?? new CreditCardCalendarSyncStatusDTO { CreditCardId = creditCardId, State = "Synced" });
    }

    public Task<IReadOnlyList<CreditCardCalendarSyncStatusDTO>> ResyncAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IReadOnlyList<CreditCardCalendarSyncStatusDTO> GetSyncStatuses() => Statuses;
}

internal sealed class StubCreditCardServiceForSettings : ICreditCardService
{
    public List<CreditCardDTO> CreditCards { get; set; } = [];
    public Exception? ThrowOnGetCreditCards { get; set; }

    public IReadOnlyList<CreditCardDTO> GetCreditCards() => ThrowOnGetCreditCards is null ? CreditCards : throw ThrowOnGetCreditCards;

    public Task<CreditCardDTO> CreateCreditCardAsync(CreditCardCreateDTO request) => throw new NotSupportedException();

    public Task<CreditCardDTO> UpdateCreditCardAsync(Guid id, CreditCardUpdateDTO request) => throw new NotSupportedException();

    public Task DeleteCreditCardAsync(Guid id) => throw new NotSupportedException();
}

internal sealed class StubBrowserLauncher : IBrowserLauncher
{
    public string? LastOpenedUrl { get; private set; }
    public int OpenUrlCallCount { get; private set; }

    public void OpenUrl(string url)
    {
        LastOpenedUrl = url;
        OpenUrlCallCount++;
    }
}
