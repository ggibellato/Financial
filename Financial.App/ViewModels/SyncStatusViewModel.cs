using System.Globalization;
using System.Windows.Threading;
using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Infrastructure.Sync;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Reads both bounded contexts' current sync status directly from their repository instances
/// (no HTTP call, since Financial.App hosts both contexts' Infrastructure in-process), refreshing
/// on construction and every <see cref="PollInterval"/> via a <see cref="DispatcherTimer"/>.
/// </summary>
public class SyncStatusViewModel : ViewModelBase
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private const string LastSuccessfulSaveFormat = "dd/MM/yyyy HH:mm";

    private readonly ICashFlowRepository _cashFlowRepository;
    private readonly IRepository _investmentRepository;
    private readonly DispatcherTimer _timer;

    private SyncStatus _cashFlowStatus = new(SyncState.Idle, null, null);
    private SyncStatus _investmentStatus = new(SyncState.Idle, null, null);

    public SyncStatusViewModel(ICashFlowRepository cashFlowRepository, IRepository investmentRepository)
    {
        _cashFlowRepository = cashFlowRepository ?? throw new ArgumentNullException(nameof(cashFlowRepository));
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));

        _timer = new DispatcherTimer { Interval = PollInterval };
        _timer.Tick += (_, _) => RefreshStatus();

        RefreshStatus();
        _timer.Start();
    }

    public SyncStatus CashFlowStatus
    {
        get => _cashFlowStatus;
        private set
        {
            if (SetProperty(ref _cashFlowStatus, value))
            {
                NotifyIndicatorPropertiesChanged();
            }
        }
    }

    public SyncStatus InvestmentStatus
    {
        get => _investmentStatus;
        private set
        {
            if (SetProperty(ref _investmentStatus, value))
            {
                NotifyIndicatorPropertiesChanged();
            }
        }
    }

    public bool IsIndicatorVisible =>
        CashFlowStatus.State == SyncState.Failed || InvestmentStatus.State == SyncState.Failed;

    public IReadOnlyList<string> IndicatorMessages
    {
        get
        {
            var messages = new List<string>();
            AppendIfFailed(messages, "CashFlow", CashFlowStatus);
            AppendIfFailed(messages, "Investment", InvestmentStatus);
            return messages;
        }
    }

    public void RefreshStatus()
    {
        CashFlowStatus = ResolveStatus(_cashFlowRepository);
        InvestmentStatus = ResolveStatus(_investmentRepository);
    }

    private static SyncStatus ResolveStatus(object repository) =>
        repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);

    private static void AppendIfFailed(List<string> messages, string contextName, SyncStatus status)
    {
        if (status.State != SyncState.Failed)
        {
            return;
        }

        var lastSuccessfulSave = status.LastSuccessfulSaveUtc is { } timestamp
            ? timestamp.ToString(LastSuccessfulSaveFormat, CultureInfo.InvariantCulture)
            : "Never";

        messages.Add(
            $"{contextName} changes could not be saved to Google Drive (last error: {status.LastError}). " +
            $"Last successful save: {lastSuccessfulSave}.");
    }

    private void NotifyIndicatorPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsIndicatorVisible));
        OnPropertyChanged(nameof(IndicatorMessages));
    }
}
