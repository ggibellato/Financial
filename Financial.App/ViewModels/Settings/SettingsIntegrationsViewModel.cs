using System.Collections.ObjectModel;
using System.Windows.Threading;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Settings;

public sealed record CalendarSyncRow(Guid CreditCardId, string Name, DateOnly DueDate, string State, string? LastError, bool IsRetrying = false)
{
    public bool CanRetry => !IsRetrying;
}

/// <summary>
/// Reads the calendar connection/sync state directly from CashFlow's in-process Application
/// services (no HTTP call - see spec.md's Technical Decisions). Connect opens the OS default
/// browser to the OAuth consent URL and polls status on a short interval
/// (<see cref="ConnectingPollInterval"/>) until connected, since no window-focus-regained
/// signal exists in this codebase to mirror the Web page's behaviour exactly.
/// </summary>
public class SettingsIntegrationsViewModel : ViewModelBase
{
    public static readonly TimeSpan ConnectingPollInterval = TimeSpan.FromSeconds(3);

    private readonly ICalendarIntegrationService _calendarIntegrationService;
    private readonly ICreditCardCalendarSyncService _calendarSyncService;
    private readonly ICreditCardService _creditCardService;
    private readonly IBrowserLauncher _browserLauncher;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsIntegrationsViewModel> _logger;

    private DispatcherTimer? _connectingTimer;
    private int _refreshRequestId;

    private bool _isLoading = true;
    private string? _error;
    private CalendarConnectionStatusDTO? _status;
    private bool _isConnecting;
    private bool _isDisconnecting;
    private string? _disconnectError;
    private Guid? _retryingCardId;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                NotifyPanelPropertiesChanged();
            }
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
                NotifyPanelPropertiesChanged();
            }
        }
    }

    public bool HasError => Error != null;

    public bool ShowContent => !IsLoading && !HasError;

    public CalendarConnectionStatusDTO? Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(NotConnectedMessage));
                OnPropertyChanged(nameof(ConnectedSinceText));
                NotifyPanelPropertiesChanged();
            }
        }
    }

    public bool IsConnected => Status?.Connected == true;

    public bool IsConnecting
    {
        get => _isConnecting;
        private set
        {
            if (SetProperty(ref _isConnecting, value))
            {
                OnPropertyChanged(nameof(ConnectButtonText));
                OnPropertyChanged(nameof(CanConnect));
            }
        }
    }

    public bool ShowNotConnectedPanel => ShowContent && !IsConnected;

    public bool ShowConnectedPanel => ShowContent && IsConnected;

    public string ConnectButtonText => IsConnecting ? "Connecting…" : "Connect Google Calendar";

    public string NotConnectedMessage => Status?.DisconnectReason == "token_revoked"
        ? "Connection lost — please reconnect."
        : "Connect your Google Calendar to get due-date reminders outside the app.";

    public string ConnectedSinceText => Status?.ConnectedAtUtc is { } connectedAtUtc
        ? connectedAtUtc.LocalDateTime.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)
        : string.Empty;

    private void NotifyPanelPropertiesChanged()
    {
        OnPropertyChanged(nameof(ShowContent));
        OnPropertyChanged(nameof(ShowNotConnectedPanel));
        OnPropertyChanged(nameof(ShowConnectedPanel));
    }

    public bool CanConnect => !IsConnecting;

    public bool IsDisconnecting
    {
        get => _isDisconnecting;
        private set
        {
            if (SetProperty(ref _isDisconnecting, value))
            {
                OnPropertyChanged(nameof(CanDisconnect));
            }
        }
    }

    public bool CanDisconnect => !IsDisconnecting;

    public string? DisconnectError
    {
        get => _disconnectError;
        private set => SetProperty(ref _disconnectError, value);
    }

    public Guid? RetryingCardId
    {
        get => _retryingCardId;
        private set => SetProperty(ref _retryingCardId, value);
    }

    public ObservableCollection<CalendarSyncRow> SyncRows { get; } = [];

    public RelayCommand RetryLoadCommand { get; }

    public RelayCommand ConnectCommand { get; }

    public RelayCommand DisconnectCommand { get; }

    public RelayCommand<CalendarSyncRow> RetryCommand { get; }

    public RelayCommand OpenCalendarLinkCommand { get; }

    public SettingsIntegrationsViewModel(
        ICalendarIntegrationService calendarIntegrationService,
        ICreditCardCalendarSyncService calendarSyncService,
        ICreditCardService creditCardService,
        IBrowserLauncher browserLauncher,
        IDialogService dialogService,
        ILogger<SettingsIntegrationsViewModel> logger)
    {
        _calendarIntegrationService = calendarIntegrationService ?? throw new ArgumentNullException(nameof(calendarIntegrationService));
        _calendarSyncService = calendarSyncService ?? throw new ArgumentNullException(nameof(calendarSyncService));
        _creditCardService = creditCardService ?? throw new ArgumentNullException(nameof(creditCardService));
        _browserLauncher = browserLauncher ?? throw new ArgumentNullException(nameof(browserLauncher));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryLoadCommand = new RelayCommand(async () => await RefreshAsync());
        ConnectCommand = new RelayCommand(Connect);
        DisconnectCommand = new RelayCommand(async () => await DisconnectAsync());
        RetryCommand = new RelayCommand<CalendarSyncRow>(async row => await RetrySyncAsync(row));
        OpenCalendarLinkCommand = new RelayCommand(OpenCalendarLink);

        _ = RefreshAsync();
    }

    internal Task RefreshAsync() => ExecuteRefreshAsync(
        () => ++_refreshRequestId,
        id => id == _refreshRequestId,
        loading => IsLoading = loading,
        error => Error = error,
        async isCurrent =>
        {
            var status = await _calendarIntegrationService.GetStatusAsync();
            if (!isCurrent())
            {
                return;
            }

            Status = status;

            var rows = await BuildSyncRowsAsync();
            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(SyncRows, rows);
        },
        ex => _logger.LogError("SettingsIntegrations refresh failed with {ErrorType}", ex.GetType().Name));

    private async Task<IEnumerable<CalendarSyncRow>> BuildSyncRowsAsync()
    {
        var creditCards = await Task.Run(() => _creditCardService.GetCreditCards());
        var syncStatuses = await Task.Run(() => _calendarSyncService.GetSyncStatuses());

        var statusByCardId = syncStatuses.ToDictionary(s => s.CreditCardId);
        return creditCards
            .Where(card => card.IsActive && card.NextInvoiceDueDate.HasValue)
            .Select(card =>
            {
                statusByCardId.TryGetValue(card.Id, out var syncStatus);
                return new CalendarSyncRow(card.Id, card.Name, card.NextInvoiceDueDate!.Value, syncStatus?.State ?? "Pending", syncStatus?.LastError);
            });
    }

    internal void Connect()
    {
        _browserLauncher.OpenUrl(_calendarIntegrationService.BuildAuthorizationUrl());
        IsConnecting = true;

        StopConnectingPoll();
        _connectingTimer = new DispatcherTimer { Interval = ConnectingPollInterval };
        _connectingTimer.Tick += async (_, _) => await PollConnectingStatusAsync();
        _connectingTimer.Start();
    }

    internal void OpenCalendarLink()
    {
        if (Status?.CalendarId is not { } calendarId)
        {
            return;
        }

        _browserLauncher.OpenUrl($"https://calendar.google.com/calendar/u/0/r?cid={Uri.EscapeDataString(calendarId)}");
    }

    internal async Task PollConnectingStatusAsync()
    {
        CalendarConnectionStatusDTO status;
        try
        {
            status = await _calendarIntegrationService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("SettingsIntegrations connecting-poll failed with {ErrorType}", ex.GetType().Name);
            return;
        }

        Status = status;

        if (status.Connected || status.DisconnectReason is not null)
        {
            IsConnecting = false;
            StopConnectingPoll();
            ReplaceAll(SyncRows, await BuildSyncRowsAsync());
        }
    }

    private void StopConnectingPoll()
    {
        _connectingTimer?.Stop();
        _connectingTimer = null;
    }

    internal async Task DisconnectAsync()
    {
        if (!_dialogService.Confirm(
            $"This will delete the \"{Status?.CalendarName ?? "Financial - Credit Card Due Dates"}\" calendar and all its events from Google Calendar. Continue?",
            "Disconnect Google Calendar"))
        {
            return;
        }

        IsDisconnecting = true;
        DisconnectError = null;

        try
        {
            await _calendarIntegrationService.DisconnectAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("SettingsIntegrations disconnect failed with {ErrorType}", ex.GetType().Name);
            DisconnectError = ex.Message;
        }
        finally
        {
            IsDisconnecting = false;
        }
    }

    internal async Task RetrySyncAsync(CalendarSyncRow? row)
    {
        if (row is null)
        {
            return;
        }

        RetryingCardId = row.CreditCardId;
        ReplaceRow(row with { IsRetrying = true });

        try
        {
            var updated = await _calendarSyncService.ResyncAsync(row.CreditCardId);
            ReplaceRow(row with { State = updated.State, LastError = updated.LastError, IsRetrying = false });
        }
        catch (Exception ex)
        {
            _logger.LogError("SettingsIntegrations resync failed with {ErrorType}", ex.GetType().Name);
            ReplaceRow(row with { IsRetrying = false });
        }
        finally
        {
            RetryingCardId = null;
        }
    }

    private void ReplaceRow(CalendarSyncRow updated)
    {
        var index = SyncRows.ToList().FindIndex(r => r.CreditCardId == updated.CreditCardId);
        if (index >= 0)
        {
            SyncRows[index] = updated;
        }
    }
}
