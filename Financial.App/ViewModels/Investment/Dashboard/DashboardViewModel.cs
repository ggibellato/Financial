using System.ComponentModel;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DashboardViewModel : ViewModelBase
{
    private bool _retryInFlight;

    public DashboardViewModel(DashboardKpiTilesViewModel kpiTiles)
    {
        KpiTiles = kpiTiles ?? throw new ArgumentNullException(nameof(kpiTiles));
        KpiTiles.PropertyChanged += OnPanelStateChanged;

        RetryAllCommand = new RelayCommand(async () =>
        {
            _retryInFlight = true;
            await LoadAllAsync();
        });

        _ = LoadAllAsync();
    }

    public event EventHandler? RecoveredFromError;

    public DashboardKpiTilesViewModel KpiTiles { get; }

    public RelayCommand RetryAllCommand { get; }

    public bool ShowPageLevelError => KpiTiles.HasError && !KpiTiles.IsLoading;

    public bool ShowPanels => !ShowPageLevelError;

    internal Task LoadAllAsync() => KpiTiles.LoadAsync();

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
        if (_retryInFlight && !KpiTiles.IsLoading)
        {
            _retryInFlight = false;
            if (!ShowPageLevelError)
            {
                RecoveredFromError?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
