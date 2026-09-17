using System.ComponentModel;
using Financial.Investment.Application.Enums;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DashboardViewModel : ViewModelBase
{
    private readonly IMainNavigationViewModel _activeTree;
    private readonly IMainNavigationViewModel _historicTree;
    private bool _retryInFlight;

    public DashboardViewModel(
        DashboardKpiTilesViewModel kpiTiles,
        AllocationBreakdownViewModel allocation,
        DataQualityWarningsViewModel warnings,
        UpcomingIncomeViewModel income,
        IMainNavigationViewModel activeTree,
        IMainNavigationViewModel historicTree)
    {
        KpiTiles = kpiTiles ?? throw new ArgumentNullException(nameof(kpiTiles));
        Allocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        Income = income ?? throw new ArgumentNullException(nameof(income));
        _activeTree = activeTree ?? throw new ArgumentNullException(nameof(activeTree));
        _historicTree = historicTree ?? throw new ArgumentNullException(nameof(historicTree));
        KpiTiles.PropertyChanged += OnPanelStateChanged;
        Allocation.PropertyChanged += OnPanelStateChanged;
        Warnings.PropertyChanged += OnPanelStateChanged;
        Income.PropertyChanged += OnPanelStateChanged;

        NavigateToHoldingCommand = new RelayCommand<WarningHoldingRef>(NavigateToHolding);

        KpiTiles.ExpandMissingPriceRequested += (_, _) => Warnings.ExpandCategory(DataQualityCategory.UnpricedOpenHoldings);
        Warnings.NavigateToHoldingRequested += (_, holding) => NavigateToHolding(holding);

        RetryAllCommand = new RelayCommand(async () =>
        {
            _retryInFlight = true;
            await LoadAllAsync();
        });

        _ = LoadAllAsync();
    }

    public event EventHandler? RecoveredFromError;

    public event EventHandler<InvestmentScope>? NavigateToTreeRequested;

    public DashboardKpiTilesViewModel KpiTiles { get; }

    public AllocationBreakdownViewModel Allocation { get; }

    public DataQualityWarningsViewModel Warnings { get; }

    public UpcomingIncomeViewModel Income { get; }

    public RelayCommand RetryAllCommand { get; }

    public RelayCommand<WarningHoldingRef> NavigateToHoldingCommand { get; }

    public bool ShowPageLevelError =>
        KpiTiles.HasError && Allocation.HasError && Warnings.HasError && Income.HasError && !AnyPanelLoading;

    public bool ShowPanels => !ShowPageLevelError;

    internal Task LoadAllAsync() =>
        Task.WhenAll(KpiTiles.LoadAsync(), Allocation.LoadAsync(), Warnings.LoadAsync(), Income.LoadAsync());

    private bool AnyPanelLoading => KpiTiles.IsLoading || Allocation.IsLoading || Warnings.IsLoading || Income.IsLoading;

    private void NavigateToHolding(WarningHoldingRef? holding)
    {
        if (holding is null)
        {
            return;
        }

        if (TrySelect(_activeTree, holding, InvestmentScope.Active) || TrySelect(_historicTree, holding, InvestmentScope.Historic))
        {
            return;
        }

        Warnings.NavigationError =
            $"Unable to locate {holding.AssetName} — it may have moved or been archived since this report was generated.";
    }

    private bool TrySelect(IMainNavigationViewModel tree, WarningHoldingRef holding, InvestmentScope scope)
    {
        if (!tree.SelectHolding(holding.BrokerName, holding.PortfolioName, holding.AssetName))
        {
            return false;
        }

        Warnings.NavigationError = null;
        NavigateToTreeRequested?.Invoke(this, scope);
        return true;
    }

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
