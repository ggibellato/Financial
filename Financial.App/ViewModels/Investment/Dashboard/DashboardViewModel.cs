using System.ComponentModel;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DashboardViewModel : ViewModelBase
{
    private readonly IMainNavigationViewModel _activeTree;
    private readonly IMainNavigationViewModel _historicTree;
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;
    private bool _retryInFlight;
    private Currency _displayCurrency;
    private Currency? _brokerCurrencyFilter;

    public DashboardViewModel(
        DashboardKpiTilesViewModel kpiTiles,
        AllocationBreakdownViewModel allocation,
        DataQualityWarningsViewModel warnings,
        UpcomingIncomeViewModel income,
        IMainNavigationViewModel activeTree,
        IMainNavigationViewModel historicTree,
        IReportingCurrencyProvider reportingCurrencyProvider)
    {
        KpiTiles = kpiTiles ?? throw new ArgumentNullException(nameof(kpiTiles));
        Allocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        Income = income ?? throw new ArgumentNullException(nameof(income));
        _activeTree = activeTree ?? throw new ArgumentNullException(nameof(activeTree));
        _historicTree = historicTree ?? throw new ArgumentNullException(nameof(historicTree));
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
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

        _displayCurrency = _reportingCurrencyProvider.GetReportingCurrency();
        _brokerCurrencyFilter = null;

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

    /// <summary>Page-local, never persisted — seeded once from the global Reporting Currency
    /// setting's stored value on construction, ignoring that setting's own enabled flag, and never
    /// re-synced afterward.</summary>
    public Currency DisplayCurrency
    {
        get => _displayCurrency;
        set
        {
            if (!SetProperty(ref _displayCurrency, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsGbpSelected));
            OnPropertyChanged(nameof(IsBrlSelected));
            OnPropertyChanged(nameof(IsUsdSelected));
            _ = ReloadFilteredPanelsAsync();
        }
    }

    public bool IsGbpSelected
    {
        get => DisplayCurrency == Currency.GBP;
        set { if (value) DisplayCurrency = Currency.GBP; }
    }

    public bool IsBrlSelected
    {
        get => DisplayCurrency == Currency.BRL;
        set { if (value) DisplayCurrency = Currency.BRL; }
    }

    public bool IsUsdSelected
    {
        get => DisplayCurrency == Currency.USD;
        set { if (value) DisplayCurrency = Currency.USD; }
    }

    /// <summary>Page-local, never persisted — always resets to "All currencies" (<see langword="null"/>)
    /// on construction, independent of <see cref="DisplayCurrency"/>.</summary>
    public Currency? BrokerCurrencyFilter
    {
        get => _brokerCurrencyFilter;
        set
        {
            if (SetProperty(ref _brokerCurrencyFilter, value))
            {
                _ = ReloadFilteredPanelsAsync();
            }
        }
    }

    public bool ShowPageLevelError =>
        KpiTiles.HasError && Allocation.HasError && Warnings.HasError && Income.HasError && !AnyPanelLoading;

    public bool ShowPanels => !ShowPageLevelError;

    internal Task LoadAllAsync() =>
        Task.WhenAll(
            KpiTiles.LoadAsync(DisplayCurrency, BrokerCurrencyFilter),
            Allocation.LoadAsync(DisplayCurrency, BrokerCurrencyFilter),
            Warnings.LoadAsync(),
            Income.LoadAsync());

    /// <summary>Only KpiTiles and Allocation react to a filter change — Warnings/Income are
    /// unaffected by either control, per PRD scope.</summary>
    private Task ReloadFilteredPanelsAsync() =>
        Task.WhenAll(
            KpiTiles.LoadAsync(DisplayCurrency, BrokerCurrencyFilter),
            Allocation.LoadAsync(DisplayCurrency, BrokerCurrencyFilter));

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

        if (holding.CorporateActionId is Guid corporateActionId)
        {
            tree.AssetDetails.FocusCorporateAction(corporateActionId);
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
