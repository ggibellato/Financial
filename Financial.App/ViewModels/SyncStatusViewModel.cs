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
        private set => SetProperty(ref _cashFlowStatus, value);
    }

    public SyncStatus InvestmentStatus
    {
        get => _investmentStatus;
        private set => SetProperty(ref _investmentStatus, value);
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
}
