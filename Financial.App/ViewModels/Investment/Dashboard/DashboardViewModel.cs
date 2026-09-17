using System.ComponentModel;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel(DashboardKpiTilesViewModel kpiTiles)
    {
        KpiTiles = kpiTiles ?? throw new ArgumentNullException(nameof(kpiTiles));
        KpiTiles.PropertyChanged += OnPanelStateChanged;

        RetryAllCommand = new RelayCommand(async () => await LoadAllAsync());

        _ = LoadAllAsync();
    }

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
    }
}
