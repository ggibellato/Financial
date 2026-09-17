using System.ComponentModel;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DashboardViewModel : ViewModelBase
{
    private bool _retryInFlight;

    public DashboardViewModel(DashboardKpiTilesViewModel kpiTiles, AllocationBreakdownViewModel allocation)
    {
        KpiTiles = kpiTiles ?? throw new ArgumentNullException(nameof(kpiTiles));
        Allocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
        KpiTiles.PropertyChanged += OnPanelStateChanged;
        Allocation.PropertyChanged += OnPanelStateChanged;

        RetryAllCommand = new RelayCommand(async () =>
        {
            _retryInFlight = true;
            await LoadAllAsync();
        });

        _ = LoadAllAsync();
    }

    public event EventHandler? RecoveredFromError;

    public DashboardKpiTilesViewModel KpiTiles { get; }

    public AllocationBreakdownViewModel Allocation { get; }

    public RelayCommand RetryAllCommand { get; }

    public bool ShowPageLevelError => KpiTiles.HasError && Allocation.HasError && !AnyPanelLoading;

    public bool ShowPanels => !ShowPageLevelError;

    internal Task LoadAllAsync() => Task.WhenAll(KpiTiles.LoadAsync(), Allocation.LoadAsync());

    private bool AnyPanelLoading => KpiTiles.IsLoading || Allocation.IsLoading;

    private void OnPanelStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(DashboardKpiTilesViewModel.IsLoading) or nameof(DashboardKpiTilesViewModel.ErrorMessage)))
        {
            return;
        }

        OnPropertyChanged(nameof(ShowPageLevelError));
        OnPropertyChanged(nameof(ShowPanels));

        // Only fire once every panel has settled (not merely started loading again), and only when
        // the retry actually recovered - if every panel still fails, the Retry button is still on
        // screen in the same place and already holds focus, so there is nothing to move.
        if (_retryInFlight && !AnyPanelLoading)
        {
            _retryInFlight = false;
            if (!ShowPageLevelError)
            {
                RecoveredFromError?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
