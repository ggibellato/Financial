using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.Services;
using Financial.Presentation.App.ViewModels.Settings;
using Financial.Presentation.Tests.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Settings;

public class SettingsIntegrationsViewModelTests
{
    private static (
        SettingsIntegrationsViewModel ViewModel,
        StubCalendarIntegrationService CalendarIntegration,
        StubCreditCardCalendarSyncService CalendarSync,
        StubCreditCardServiceForSettings CreditCards,
        StubBrowserLauncher BrowserLauncher,
        StubCalendarOAuthCallbackListener CallbackListener,
        StubDialogService Dialog) CreateViewModel()
    {
        var calendarIntegration = new StubCalendarIntegrationService();
        var calendarSync = new StubCreditCardCalendarSyncService();
        var creditCards = new StubCreditCardServiceForSettings();
        var browserLauncher = new StubBrowserLauncher();
        var callbackListener = new StubCalendarOAuthCallbackListener();
        var dialog = new StubDialogService();
        var viewModel = new SettingsIntegrationsViewModel(
            calendarIntegration, calendarSync, creditCards, browserLauncher, callbackListener, dialog,
            new RecordingLogger<SettingsIntegrationsViewModel>());
        return (viewModel, calendarIntegration, calendarSync, creditCards, browserLauncher, callbackListener, dialog);
    }

    private static CreditCardDTO CreditCard(Guid id, string name, bool isActive = true, DateOnly? nextInvoiceDueDate = null, bool hasReferences = false) => new()
    {
        Id = id,
        Name = name,
        IsActive = isActive,
        NextInvoiceDueDate = nextInvoiceDueDate,
        HasReferences = hasReferences,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesStatus()
    {
        var (viewModel, calendarIntegration, _, _, _, _, _) = CreateViewModel();
        calendarIntegration.StatusToReturn = new CalendarConnectionStatusDTO
        {
            Connected = true,
            AccountEmail = "user@gmail.com",
            CalendarName = "Financial - Credit Card Due Dates",
            CalendarId = "cal-1",
            ConnectedAtUtc = DateTimeOffset.UtcNow,
        };

        await viewModel.RefreshAsync();

        viewModel.IsConnected.Should().BeTrue();
        viewModel.Status!.AccountEmail.Should().Be("user@gmail.com");
    }

    [Fact]
    public async Task RefreshAsync_ServiceThrows_SetsErrorAndLogsFailure()
    {
        var (viewModel, calendarIntegration, _, _, _, _, _) = CreateViewModel();
        calendarIntegration.ThrowOnGetStatus = new InvalidOperationException("boom");

        await viewModel.RefreshAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.Error.Should().Be("boom");
    }

    [Fact]
    public async Task RefreshAsync_JoinsActiveDueDatedCardsWithSyncStatus_DefaultingAbsentStatusToPending()
    {
        var (viewModel, _, calendarSync, creditCards, _, _, _) = CreateViewModel();
        var syncedId = Guid.NewGuid();
        var neverSyncedId = Guid.NewGuid();
        var noDueDateId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        creditCards.CreditCards =
        [
            CreditCard(syncedId, "BaAmex", nextInvoiceDueDate: new DateOnly(2026, 9, 10)),
            CreditCard(neverSyncedId, "Nubank", nextInvoiceDueDate: new DateOnly(2026, 9, 15)),
            CreditCard(noDueDateId, "PaypalCredit", nextInvoiceDueDate: null),
            CreditCard(inactiveId, "OldCard", isActive: false, nextInvoiceDueDate: new DateOnly(2026, 9, 1)),
        ];
        calendarSync.Statuses = [new CreditCardCalendarSyncStatusDTO { CreditCardId = syncedId, State = "Synced" }];

        await viewModel.RefreshAsync();

        viewModel.SyncRows.Should().HaveCount(2);
        viewModel.SyncRows.Should().Contain(r => r.CreditCardId == syncedId && r.State == "Synced");
        viewModel.SyncRows.Should().Contain(r => r.CreditCardId == neverSyncedId && r.State == "Pending");
        viewModel.SyncRows.Should().NotContain(r => r.CreditCardId == noDueDateId);
        viewModel.SyncRows.Should().NotContain(r => r.CreditCardId == inactiveId);
    }

    [Fact]
    public async Task RefreshAsync_ASlowerStaleRefreshInFlight_NeverClobbersALaterFasterRefresh()
    {
        // Sequenced deterministically, with no reliance on thread-pool scheduling order between
        // two overlapping calls (that ambiguity is exactly what made an earlier version of this
        // test hang in CI): let the constructor's own initial refresh fully settle first, then run
        // two *explicit*, directly-awaitable RefreshAsync() calls whose ordering is fully test-controlled.
        var calendarIntegration = new StubCalendarIntegrationService();
        var calendarSync = new StubCreditCardCalendarSyncService();
        var creditCards = new StubCreditCardServiceForSettings();
        var viewModel = new SettingsIntegrationsViewModel(
            calendarIntegration, calendarSync, creditCards, new StubBrowserLauncher(), new StubCalendarOAuthCallbackListener(),
            new StubDialogService(), new RecordingLogger<SettingsIntegrationsViewModel>());
        await WaitUntilAsync(() => !viewModel.IsLoading);

        // Request A ("stale"): gate GetStatusAsync (a plain awaited Task, not a Task.Run - a fully
        // deterministic suspension point) and GetCreditCards, so once released it captures an EMPTY
        // snapshot, since no card data has been set yet.
        var pendingStatus = new TaskCompletionSource<CalendarConnectionStatusDTO>();
        calendarIntegration.PendingGetStatus = pendingStatus;
        var creditCardsGate = new SemaphoreSlim(0, 1);
        creditCards.BlockFirstCallUntilReleased = creditCardsGate;

        var staleRefreshTask = viewModel.RefreshAsync();
        pendingStatus.SetResult(new CalendarConnectionStatusDTO { Connected = false });
        await WaitUntilAsync(() => creditCards.HasEnteredBlock);
        // Request A is now blocked inside GetCreditCards(), having already captured its empty snapshot.

        // Request B ("fresh"): fully unblocked, completes normally with real data.
        var cardId = Guid.NewGuid();
        creditCards.CreditCards = [CreditCard(cardId, "BaAmex", nextInvoiceDueDate: new DateOnly(2026, 9, 10))];
        calendarSync.Statuses = [new CreditCardCalendarSyncStatusDTO { CreditCardId = cardId, State = "Synced" }];
        await viewModel.RefreshAsync();
        viewModel.SyncRows.Should().ContainSingle(r => r.CreditCardId == cardId);

        // Release request A - its stale (empty) write attempt must be discarded, not re-applied.
        creditCardsGate.Release();
        await staleRefreshTask;

        viewModel.SyncRows.Should().ContainSingle(r => r.CreditCardId == cardId);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task ConnectAsync_OpensBrowserWithAuthorizationUrl_AwaitsTheLoopbackListener_AndCompletesTheConnection()
    {
        var (viewModel, calendarIntegration, calendarSync, _, browserLauncher, callbackListener, _) = CreateViewModel();
        calendarIntegration.AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth?state=abc&redirect_uri=http%3A%2F%2Flocalhost%3A8082%2F";
        callbackListener.ResultToReturn = new CalendarOAuthCallbackResult("auth-code", "abc", null);
        calendarIntegration.CompleteConnectionResult = new CalendarCallbackResultDTO { Success = true };

        await viewModel.ConnectAsync();

        browserLauncher.LastOpenedUrl.Should().Be(calendarIntegration.AuthorizationUrl);
        callbackListener.LastAuthorizationUrl.Should().Be(calendarIntegration.AuthorizationUrl);
        calendarIntegration.LastCompleteConnectionArgs.Should().Be(("auth-code", "abc", (string?)null));
        viewModel.IsConnecting.Should().BeFalse();
        viewModel.ConnectError.Should().BeNull();
        calendarSync.ResyncAllCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ConnectAsync_WhenCompleteConnectionFails_DoesNotTriggerResyncAll()
    {
        var (viewModel, calendarIntegration, calendarSync, _, _, _, _) = CreateViewModel();
        calendarIntegration.CompleteConnectionResult = new CalendarCallbackResultDTO { Success = false, ErrorMessage = "State mismatch." };

        await viewModel.ConnectAsync();

        calendarSync.ResyncAllCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ConnectAsync_WhileListenerIsAwaitingTheCallback_KeepsIsConnectingTrue()
    {
        var (viewModel, _, _, _, _, callbackListener, _) = CreateViewModel();
        var pending = new TaskCompletionSource<CalendarOAuthCallbackResult>();
        callbackListener.PendingListen = pending;

        var connectTask = viewModel.ConnectAsync();
        await WaitUntilAsync(() => viewModel.IsConnecting);

        viewModel.IsConnecting.Should().BeTrue();

        pending.SetResult(new CalendarOAuthCallbackResult("auth-code", "state", null));
        await connectTask;

        viewModel.IsConnecting.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhenCompleteConnectionFails_SurfacesConnectError()
    {
        var (viewModel, calendarIntegration, _, _, _, _, _) = CreateViewModel();
        calendarIntegration.CompleteConnectionResult = new CalendarCallbackResultDTO { Success = false, ErrorMessage = "State mismatch." };

        await viewModel.ConnectAsync();

        viewModel.ConnectError.Should().Be("State mismatch.");
        viewModel.IsConnecting.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhenTheListenerThrows_SurfacesConnectErrorAndLogsFailure()
    {
        var (viewModel, _, _, _, _, callbackListener, _) = CreateViewModel();
        callbackListener.ThrowOnListen = new InvalidOperationException("Could not bind the loopback listener.");

        await viewModel.ConnectAsync();

        viewModel.ConnectError.Should().NotBeNullOrEmpty();
        viewModel.IsConnecting.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhileAlreadyConnecting_IsANoOp()
    {
        var (viewModel, _, _, _, browserLauncher, callbackListener, _) = CreateViewModel();
        var pending = new TaskCompletionSource<CalendarOAuthCallbackResult>();
        callbackListener.PendingListen = pending;

        var firstConnect = viewModel.ConnectAsync();
        await WaitUntilAsync(() => viewModel.IsConnecting);
        await viewModel.ConnectAsync();

        browserLauncher.OpenUrlCallCount.Should().Be(1);

        pending.SetResult(new CalendarOAuthCallbackResult("auth-code", "state", null));
        await firstConnect;
    }

    [Fact]
    public async Task DisconnectAsync_AlwaysConfirmsFirst()
    {
        var (viewModel, calendarIntegration, _, _, _, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;

        await viewModel.DisconnectAsync();

        dialog.LastConfirmMessage.Should().NotBeNull();
        calendarIntegration.DisconnectCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DisconnectAsync_Confirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, calendarIntegration, _, _, _, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = true;

        await viewModel.DisconnectAsync();

        calendarIntegration.DisconnectCallCount.Should().Be(1);
        calendarIntegration.GetStatusCallCount.Should().BeGreaterThan(0);
        viewModel.IsDisconnecting.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_ServiceThrows_SurfacesDisconnectError()
    {
        var (viewModel, calendarIntegration, _, _, _, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = true;
        calendarIntegration.ThrowOnDisconnect = new InvalidOperationException("Disconnect failed.");

        await viewModel.DisconnectAsync();

        viewModel.DisconnectError.Should().Be("Disconnect failed.");
        viewModel.IsDisconnecting.Should().BeFalse();
    }

    [Fact]
    public async Task RetrySyncAsync_CallsResyncForTheCorrectCard_AndUpdatesOnlyThatRow()
    {
        var (viewModel, _, calendarSync, creditCards, _, _, _) = CreateViewModel();
        var errorId = Guid.NewGuid();
        var otherSyncedId = Guid.NewGuid();
        creditCards.CreditCards =
        [
            CreditCard(errorId, "Nubank", nextInvoiceDueDate: new DateOnly(2026, 9, 15)),
            CreditCard(otherSyncedId, "BaAmex", nextInvoiceDueDate: new DateOnly(2026, 9, 10)),
        ];
        calendarSync.Statuses =
        [
            new CreditCardCalendarSyncStatusDTO { CreditCardId = errorId, State = "Error", LastError = "Rate limit exceeded" },
            new CreditCardCalendarSyncStatusDTO { CreditCardId = otherSyncedId, State = "Synced" },
        ];
        await viewModel.RefreshAsync();
        calendarSync.ResyncResult = new CreditCardCalendarSyncStatusDTO { CreditCardId = errorId, State = "Synced" };
        var row = viewModel.SyncRows.Single(r => r.CreditCardId == errorId);

        await viewModel.RetrySyncAsync(row);

        calendarSync.LastResyncedCardId.Should().Be(errorId);
        viewModel.SyncRows.Single(r => r.CreditCardId == errorId).State.Should().Be("Synced");
        viewModel.SyncRows.Single(r => r.CreditCardId == otherSyncedId).State.Should().Be("Synced");
        viewModel.RetryingCardId.Should().BeNull();
    }

    [Fact]
    public async Task RetrySyncAsync_ServiceThrows_ClearsIsRetryingWithoutChangingState()
    {
        var (viewModel, _, calendarSync, creditCards, _, _, _) = CreateViewModel();
        var cardId = Guid.NewGuid();
        creditCards.CreditCards = [CreditCard(cardId, "Nubank", nextInvoiceDueDate: new DateOnly(2026, 9, 15))];
        calendarSync.Statuses = [new CreditCardCalendarSyncStatusDTO { CreditCardId = cardId, State = "Error", LastError = "Rate limit exceeded" }];
        await viewModel.RefreshAsync();
        calendarSync.ThrowOnResync = new InvalidOperationException("Sync failed.");
        var row = viewModel.SyncRows.Single();

        await viewModel.RetrySyncAsync(row);

        var updatedRow = viewModel.SyncRows.Single();
        updatedRow.IsRetrying.Should().BeFalse();
        updatedRow.CanRetry.Should().BeTrue();
        updatedRow.State.Should().Be("Error");
        viewModel.RetryingCardId.Should().BeNull();
    }

    [Fact]
    public async Task RetrySyncAsync_TracksRetryingCardId_WhileInFlight()
    {
        var (viewModel, _, calendarSync, creditCards, _, _, _) = CreateViewModel();
        var cardId = Guid.NewGuid();
        creditCards.CreditCards = [CreditCard(cardId, "Nubank", nextInvoiceDueDate: new DateOnly(2026, 9, 15))];
        await viewModel.RefreshAsync();
        var row = viewModel.SyncRows.Single();
        var tcs = new TaskCompletionSource<CreditCardCalendarSyncStatusDTO>();
        calendarSync.PendingResync = tcs;

        var task = viewModel.RetrySyncAsync(row);
        viewModel.RetryingCardId.Should().Be(cardId);
        viewModel.SyncRows.Single().IsRetrying.Should().BeTrue();
        viewModel.SyncRows.Single().CanRetry.Should().BeFalse();

        tcs.SetResult(new CreditCardCalendarSyncStatusDTO { CreditCardId = cardId, State = "Synced" });
        await task;

        viewModel.RetryingCardId.Should().BeNull();
        viewModel.SyncRows.Single().IsRetrying.Should().BeFalse();
        viewModel.SyncRows.Single().CanRetry.Should().BeTrue();
    }

    [Fact]
    public async Task OpenCalendarLink_OpensGoogleCalendarDeepLinkForTheConnectedCalendarId()
    {
        var (viewModel, calendarIntegration, _, _, browserLauncher, _, _) = CreateViewModel();
        calendarIntegration.StatusToReturn = new CalendarConnectionStatusDTO { Connected = true, CalendarId = "abc123@group.calendar.google.com" };
        await viewModel.RefreshAsync();

        viewModel.OpenCalendarLink();

        browserLauncher.LastOpenedUrl.Should().Be("https://calendar.google.com/calendar/u/0/r?cid=abc123%40group.calendar.google.com");
    }

    [Fact]
    public void OpenCalendarLink_NoCalendarId_DoesNothing()
    {
        var (viewModel, _, _, _, browserLauncher, _, _) = CreateViewModel();

        viewModel.OpenCalendarLink();

        browserLauncher.OpenUrlCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RetrySyncAsync_NullRow_ReturnsWithoutCallingService()
    {
        var (viewModel, _, calendarSync, _, _, _, _) = CreateViewModel();

        await viewModel.RetrySyncAsync(null);

        calendarSync.LastResyncedCardId.Should().BeNull();
    }
}
