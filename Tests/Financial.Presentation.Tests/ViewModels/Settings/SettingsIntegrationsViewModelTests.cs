using Financial.CashFlow.Application.DTOs;
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
        StubDialogService Dialog) CreateViewModel()
    {
        var calendarIntegration = new StubCalendarIntegrationService();
        var calendarSync = new StubCreditCardCalendarSyncService();
        var creditCards = new StubCreditCardServiceForSettings();
        var browserLauncher = new StubBrowserLauncher();
        var dialog = new StubDialogService();
        var viewModel = new SettingsIntegrationsViewModel(
            calendarIntegration, calendarSync, creditCards, browserLauncher, dialog,
            new RecordingLogger<SettingsIntegrationsViewModel>());
        return (viewModel, calendarIntegration, calendarSync, creditCards, browserLauncher, dialog);
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
        var (viewModel, calendarIntegration, _, _, _, _) = CreateViewModel();
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
        var (viewModel, calendarIntegration, _, _, _, _) = CreateViewModel();
        calendarIntegration.ThrowOnGetStatus = new InvalidOperationException("boom");

        await viewModel.RefreshAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.Error.Should().Be("boom");
    }

    [Fact]
    public async Task RefreshAsync_JoinsActiveDueDatedCardsWithSyncStatus_DefaultingAbsentStatusToPending()
    {
        var (viewModel, _, calendarSync, creditCards, _, _) = CreateViewModel();
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
    public void Connect_OpensBrowserWithAuthorizationUrl_AndArmsConnectingState()
    {
        var (viewModel, calendarIntegration, _, _, browserLauncher, _) = CreateViewModel();
        calendarIntegration.AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth?state=abc";

        viewModel.Connect();

        browserLauncher.LastOpenedUrl.Should().Be("https://accounts.google.com/o/oauth2/v2/auth?state=abc");
        viewModel.IsConnecting.Should().BeTrue();
    }

    [Fact]
    public async Task PollConnectingStatusAsync_WhenConnected_ClearsIsConnecting()
    {
        var (viewModel, calendarIntegration, _, _, _, _) = CreateViewModel();
        viewModel.Connect();
        calendarIntegration.StatusToReturn = new CalendarConnectionStatusDTO { Connected = true, AccountEmail = "user@gmail.com" };

        await viewModel.PollConnectingStatusAsync();

        viewModel.IsConnecting.Should().BeFalse();
        viewModel.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task PollConnectingStatusAsync_StillNotConnected_KeepsIsConnectingTrue()
    {
        var (viewModel, calendarIntegration, _, _, _, _) = CreateViewModel();
        viewModel.Connect();
        calendarIntegration.StatusToReturn = new CalendarConnectionStatusDTO { Connected = false };

        await viewModel.PollConnectingStatusAsync();

        viewModel.IsConnecting.Should().BeTrue();
    }

    [Fact]
    public async Task PollConnectingStatusAsync_TokenRevoked_ClearsIsConnecting()
    {
        var (viewModel, calendarIntegration, _, _, _, _) = CreateViewModel();
        viewModel.Connect();
        calendarIntegration.StatusToReturn = new CalendarConnectionStatusDTO { Connected = false, DisconnectReason = "token_revoked" };

        await viewModel.PollConnectingStatusAsync();

        viewModel.IsConnecting.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_AlwaysConfirmsFirst()
    {
        var (viewModel, calendarIntegration, _, _, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;

        await viewModel.DisconnectAsync();

        dialog.LastConfirmMessage.Should().NotBeNull();
        calendarIntegration.DisconnectCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DisconnectAsync_Confirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, calendarIntegration, _, _, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = true;

        await viewModel.DisconnectAsync();

        calendarIntegration.DisconnectCallCount.Should().Be(1);
        calendarIntegration.GetStatusCallCount.Should().BeGreaterThan(0);
        viewModel.IsDisconnecting.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_ServiceThrows_SurfacesDisconnectError()
    {
        var (viewModel, calendarIntegration, _, _, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = true;
        calendarIntegration.ThrowOnDisconnect = new InvalidOperationException("Disconnect failed.");

        await viewModel.DisconnectAsync();

        viewModel.DisconnectError.Should().Be("Disconnect failed.");
        viewModel.IsDisconnecting.Should().BeFalse();
    }

    [Fact]
    public async Task RetrySyncAsync_CallsResyncForTheCorrectCard_AndUpdatesOnlyThatRow()
    {
        var (viewModel, _, calendarSync, creditCards, _, _) = CreateViewModel();
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
    public async Task RetrySyncAsync_TracksRetryingCardId_WhileInFlight()
    {
        var (viewModel, _, calendarSync, creditCards, _, _) = CreateViewModel();
        var cardId = Guid.NewGuid();
        creditCards.CreditCards = [CreditCard(cardId, "Nubank", nextInvoiceDueDate: new DateOnly(2026, 9, 15))];
        await viewModel.RefreshAsync();
        var row = viewModel.SyncRows.Single();
        var tcs = new TaskCompletionSource<CreditCardCalendarSyncStatusDTO>();
        calendarSync.PendingResync = tcs;

        var task = viewModel.RetrySyncAsync(row);
        viewModel.RetryingCardId.Should().Be(cardId);

        tcs.SetResult(new CreditCardCalendarSyncStatusDTO { CreditCardId = cardId, State = "Synced" });
        await task;

        viewModel.RetryingCardId.Should().BeNull();
    }

    [Fact]
    public async Task RetrySyncAsync_NullRow_ReturnsWithoutCallingService()
    {
        var (viewModel, _, calendarSync, _, _, _) = CreateViewModel();

        await viewModel.RetrySyncAsync(null);

        calendarSync.LastResyncedCardId.Should().BeNull();
    }
}
