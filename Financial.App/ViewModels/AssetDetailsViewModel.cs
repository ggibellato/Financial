using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using OxyPlot;
using System.Collections.ObjectModel;
using System.Windows;

namespace Financial.Presentation.App.ViewModels;

public class AssetDetailsViewModel : ViewModelBase, IAssetDetailsViewModel
{
    private readonly ITransactionService _transactionService;
    private readonly ICreditService _creditService;
    private readonly IAssetPriceService _assetPriceService;
    private readonly TodayInfoTracker _todayInfo;
    private readonly TransactionActions _transactionActions;
    private readonly CreditActions _creditActions;
    private readonly RelayCommand _addTransactionCommand;
    private readonly RelayCommand _updateTransactionCommand;
    private readonly RelayCommand _deleteTransactionCommand;
    private readonly RelayCommand _addCreditCommand;
    private readonly RelayCommand _updateCreditCommand;
    private readonly RelayCommand _deleteCreditCommand;
    private readonly RelayCommand _refreshTodayInfoCommand;
    private readonly RelayCommand _copyAssetNameCommand;
    private readonly RelayCommand _selectCreditsFilterCommand;
    private readonly RelayCommand _selectCreditsTypeModeCommand;
    private string _assetName = string.Empty;
    private string _brokerName = string.Empty;
    private string _portfolioName = string.Empty;
    private string _ticker = string.Empty;
    private string _isin = string.Empty;
    private string _exchange = string.Empty;
    private CountryCode _country = CountryCode.Unknown;
    private string _localTypeCode = string.Empty;
    private GlobalAssetClass _class = GlobalAssetClass.Unknown;
    private decimal _quantity;
    private decimal _averagePrice;
    private bool _isActive;
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalCredits;
    private decimal _todayCurrentValue;
    private string _todayCurrentValueAsOf = string.Empty;
    private string _todayInfoMessage = string.Empty;
    private PlotModel? _creditsPlotModel;
    private CreditsFilter _selectedCreditsFilter = CreditsFilter.LastYear;
    private CreditsTypeChartMode _selectedCreditsTypeMode = CreditsTypeChartMode.Stacked;
    private const string DefaultCreditsContextKey = "default";
    private readonly Dictionary<string, CreditsViewState> _creditsViewStateByKey = new(StringComparer.OrdinalIgnoreCase);
    private string _creditsContextKey = DefaultCreditsContextKey;
    private double _creditsPlotWidth;
    private bool _isCreditsAggregateView;
    private bool _hasCreditsContext;
    private TransactionDTO? _selectedTransaction;
    private CreditDTO? _selectedCredit;
    private IReadOnlyList<CreditsMonthTypeTotals> _creditsChartMonths = Array.Empty<CreditsMonthTypeTotals>();
    private IReadOnlyList<string> _creditsChartTypes = Array.Empty<string>();

    public string AssetName { get => _assetName; private set => SetProperty(ref _assetName, value); }
    public string BrokerName { get => _brokerName; private set => SetProperty(ref _brokerName, value); }
    public string PortfolioName { get => _portfolioName; private set => SetProperty(ref _portfolioName, value); }
    public string Ticker { get => _ticker; private set => SetProperty(ref _ticker, value); }
    public string ISIN { get => _isin; private set => SetProperty(ref _isin, value); }
    public string Exchange { get => _exchange; private set => SetProperty(ref _exchange, value); }
    public CountryCode Country { get => _country; private set => SetProperty(ref _country, value); }
    public string LocalTypeCode { get => _localTypeCode; private set => SetProperty(ref _localTypeCode, value); }
    public GlobalAssetClass Class { get => _class; private set => SetProperty(ref _class, value); }

    public decimal Quantity
    {
        get => _quantity;
        private set { if (SetProperty(ref _quantity, value)) NotifyCurrentValueChanged(); }
    }

    public decimal AveragePrice
    {
        get => _averagePrice;
        private set { if (SetProperty(ref _averagePrice, value)) NotifyCurrentValueChanged(); }
    }

    public bool IsActive { get => _isActive; private set => SetProperty(ref _isActive, value); }

    public decimal TotalBought
    {
        get => _totalBought;
        private set { if (SetProperty(ref _totalBought, value)) OnPropertyChanged(nameof(Balance)); }
    }

    public decimal TotalSold
    {
        get => _totalSold;
        private set { if (SetProperty(ref _totalSold, value)) OnPropertyChanged(nameof(Balance)); }
    }

    public decimal TotalCredits
    {
        get => _totalCredits;
        private set { if (SetProperty(ref _totalCredits, value)) NotifyCurrentValueChanged(); }
    }

    public PlotModel? CreditsPlotModel { get => _creditsPlotModel; private set => SetProperty(ref _creditsPlotModel, value); }

    public bool IsCreditsAggregateView
    {
        get => _isCreditsAggregateView;
        private set { if (SetProperty(ref _isCreditsAggregateView, value)) OnPropertyChanged(nameof(IsCreditsAssetView)); }
    }

    public bool HasCreditsContext
    {
        get => _hasCreditsContext;
        private set
        {
            if (SetProperty(ref _hasCreditsContext, value))
            {
                OnPropertyChanged(nameof(IsCreditsAssetView));
                OnPropertyChanged(nameof(ShouldShowEmptyState));
            }
        }
    }

    public bool IsCreditsAssetView => HasCreditsContext && !IsCreditsAggregateView;
    public bool ShouldShowEmptyState => !HasCreditsContext;
    public decimal Balance => TotalBought - TotalSold;

    public decimal TodayCurrentValue
    {
        get => _todayCurrentValue;
        private set { if (SetProperty(ref _todayCurrentValue, value)) NotifyCurrentValueChanged(); }
    }

    public string TodayCurrentValueAsOf { get => _todayCurrentValueAsOf; private set => SetProperty(ref _todayCurrentValueAsOf, value); }
    public string TodayInfoMessage { get => _todayInfoMessage; private set => SetProperty(ref _todayInfoMessage, value); }

    public decimal TotalCurrentValue => AssetDetailsCalculations.CalculateTotalCurrentValue(TodayCurrentValue, Quantity);
    public decimal ResultPercent => AssetDetailsCalculations.CalculateResultPercent(AveragePrice, Quantity, TotalCurrentValue);
    public decimal TotalCurrentValueWithCredits => TotalCurrentValue + TotalCredits;
    public decimal ResultPercentWithCredits => AssetDetailsCalculations.CalculateResultPercentWithCredits(AveragePrice, Quantity, TotalCurrentValueWithCredits);
    public bool HasAveragePrice => AssetDetailsCalculations.HasAveragePrice(AveragePrice, Quantity);

    public ObservableCollection<TransactionDTO> Transactions { get; } = new();
    public ObservableCollection<CreditDTO> Credits { get; } = new();
    public ObservableCollection<KeyValuePair<string, decimal>> CreditsByMonthChart { get; } = new();
    public ObservableCollection<CreditsFilterOptionViewModel> CreditsFilters { get; } = new();
    public ObservableCollection<CreditsTypeModeOptionViewModel> CreditsTypeModes { get; } = new();

    public TransactionDTO? SelectedTransaction
    {
        get => _selectedTransaction;
        set { if (SetProperty(ref _selectedTransaction, value)) UpdateCommandStates(); }
    }

    public CreditDTO? SelectedCredit
    {
        get => _selectedCredit;
        set { if (SetProperty(ref _selectedCredit, value)) UpdateCommandStates(); }
    }

    public RelayCommand AddTransactionCommand => _addTransactionCommand;
    public RelayCommand UpdateTransactionCommand => _updateTransactionCommand;
    public RelayCommand DeleteTransactionCommand => _deleteTransactionCommand;
    public RelayCommand AddCreditCommand => _addCreditCommand;
    public RelayCommand UpdateCreditCommand => _updateCreditCommand;
    public RelayCommand DeleteCreditCommand => _deleteCreditCommand;
    public RelayCommand RefreshTodayInfoCommand => _refreshTodayInfoCommand;
    public RelayCommand CopyAssetNameCommand => _copyAssetNameCommand;
    public RelayCommand SelectCreditsFilterCommand => _selectCreditsFilterCommand;
    public RelayCommand SelectCreditsTypeModeCommand => _selectCreditsTypeModeCommand;

    public AssetDetailsViewModel(
        ITransactionService transactionService,
        ICreditService creditService,
        IAssetPriceService assetPriceService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _todayInfo = new TodayInfoTracker(ApplyTodayInfo, ResetTodayInfo, UpdateCommandStates);
        _transactionActions = new TransactionActions(
            _transactionService,
            () => HasAssetContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            LoadAssetDetails,
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _creditActions = new CreditActions(
            _creditService,
            () => HasAssetContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            LoadAssetDetails,
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _addTransactionCommand = new RelayCommand(AddTransaction, CanEditTransactions);
        _updateTransactionCommand = new RelayCommand(UpdateTransaction, CanUpdateTransaction);
        _deleteTransactionCommand = new RelayCommand(DeleteTransaction, CanDeleteTransaction);
        _addCreditCommand = new RelayCommand(AddCredit, CanEditCredits);
        _updateCreditCommand = new RelayCommand(UpdateCredit, CanUpdateCredit);
        _deleteCreditCommand = new RelayCommand(DeleteCredit, CanDeleteCredit);
        _refreshTodayInfoCommand = new RelayCommand(RefreshTodayInfo, CanRefreshTodayInfo);
        _copyAssetNameCommand = new RelayCommand(CopyAssetName, CanCopyAssetName);
        _selectCreditsFilterCommand = new RelayCommand(SelectCreditsFilter);
        _selectCreditsTypeModeCommand = new RelayCommand(SelectCreditsTypeMode);
        InitializeCreditsFilters();
        InitializeCreditsTypeModes();
    }

    public void LoadAssetDetails(AssetDetailsDTO details)
    {
        var assetKey = BuildAssetKey(details.BrokerName, details.PortfolioName, details.Name);
        _todayInfo.UpdateAssetKey(assetKey);

        AssetName = details.Name;
        BrokerName = details.BrokerName;
        PortfolioName = details.PortfolioName;
        Ticker = details.Ticker;
        ISIN = details.ISIN;
        Exchange = details.Exchange;
        Country = details.Country;
        LocalTypeCode = details.LocalTypeCode;
        Class = details.Class;
        Quantity = details.Quantity;
        AveragePrice = details.AveragePrice;
        IsActive = details.IsActive;
        TotalBought = details.TotalBought;
        TotalSold = details.TotalSold;
        TotalCredits = details.TotalCredits;
        IsCreditsAggregateView = false;
        HasCreditsContext = true;
        SetCreditsContext(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), rebuild: false);

        Transactions.Clear();
        foreach (var tx in details.Transactions)
            Transactions.Add(tx);

        Credits.Clear();
        foreach (var credit in details.Credits)
            Credits.Add(credit);
        ApplyCreditsFilter();

        SelectedTransaction = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    public void Clear()
    {
        ClearAssetContext();
        Credits.Clear();
        CreditsByMonthChart.Clear();
        CreditsPlotModel = null;
        IsCreditsAggregateView = false;
        HasCreditsContext = false;
        _creditsContextKey = DefaultCreditsContextKey;
        SelectedTransaction = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    public void LoadBrokerCredits(string brokerName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        LoadAggregateCredits(BuildBrokerKey(brokerName), summary, credits);
    }

    public void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        LoadAggregateCredits(BuildPortfolioKey(brokerName, portfolioName), summary, credits);
    }

    private bool HasAssetContext =>
        !string.IsNullOrWhiteSpace(BrokerName) &&
        !string.IsNullOrWhiteSpace(PortfolioName) &&
        !string.IsNullOrWhiteSpace(AssetName);

    private bool CanEditTransactions() => HasAssetContext;
    private bool CanUpdateTransaction(object? parameter) => HasAssetContext && (parameter is TransactionDTO || SelectedTransaction != null);
    private bool CanDeleteTransaction(object? parameter) => HasAssetContext && (parameter is TransactionDTO || SelectedTransaction != null);
    private bool CanEditCredits() => HasAssetContext;
    private bool CanUpdateCredit(object? parameter) => HasAssetContext && (parameter is CreditDTO || SelectedCredit != null);
    private bool CanDeleteCredit(object? parameter) => HasAssetContext && (parameter is CreditDTO || SelectedCredit != null);
    private bool CanRefreshTodayInfo() => _todayInfo.CanRefresh(HasAssetContext);
    private bool CanCopyAssetName() => HasAssetContext;

    private void CopyAssetName()
    {
        if (!HasAssetContext)
        {
            MessageBox.Show("Select an asset before copying.", "Copy Asset", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Clipboard.SetText(AssetName);
    }

    public Task EnsureTodayInfoLoadedAsync() => RefreshTodayInfoAsync(forceRefresh: false);

    private async void RefreshTodayInfo() => await RefreshTodayInfoAsync(forceRefresh: true);

    private Task RefreshTodayInfoAsync(bool forceRefresh)
    {
        return _todayInfo.RefreshAsync(
            forceRefresh, HasAssetContext, _assetPriceService,
            Exchange, Ticker, message => TodayInfoMessage = message);
    }

    private void ResetTodayInfo()
    {
        TodayCurrentValue = 0;
        TodayCurrentValueAsOf = string.Empty;
        TodayInfoMessage = string.Empty;
    }

    private void ApplyTodayInfo(TodayInfoSnapshot snapshot)
    {
        TodayCurrentValue = snapshot.Price;
        TodayCurrentValueAsOf = snapshot.AsOf;
        TodayInfoMessage = string.Empty;
    }

    private void NotifyCurrentValueChanged()
    {
        OnPropertyChanged(nameof(TotalCurrentValue));
        OnPropertyChanged(nameof(ResultPercent));
        OnPropertyChanged(nameof(HasAveragePrice));
        OnPropertyChanged(nameof(TotalCurrentValueWithCredits));
        OnPropertyChanged(nameof(ResultPercentWithCredits));
    }

    private void LoadAggregateCredits(string contextKey, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        ClearAssetContext();
        TotalBought = summary.TotalBought;
        TotalSold = summary.TotalSold;
        IsCreditsAggregateView = true;
        HasCreditsContext = true;
        SetCreditsContext(contextKey, rebuild: false);

        Credits.Clear();
        foreach (var credit in credits)
            Credits.Add(credit);
        TotalCredits = credits.Sum(credit => credit.Value);
        ApplyCreditsFilter();

        SelectedTransaction = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    private void ClearAssetContext()
    {
        AssetName = string.Empty;
        BrokerName = string.Empty;
        PortfolioName = string.Empty;
        Ticker = string.Empty;
        ISIN = string.Empty;
        Exchange = string.Empty;
        Country = CountryCode.Unknown;
        LocalTypeCode = string.Empty;
        Class = GlobalAssetClass.Unknown;
        Quantity = 0;
        AveragePrice = 0;
        IsActive = false;
        TotalBought = 0;
        TotalSold = 0;
        TotalCredits = 0;
        _todayInfo.Clear();
        Transactions.Clear();
    }

    public void UpdateCreditsPlotWidth(double plotWidth)
    {
        if (plotWidth <= 0 || CreditsPlotModel == null) return;
        _creditsPlotWidth = plotWidth;
        CreditsChartBuilder.ApplyLabelDensity(CreditsPlotModel, _creditsPlotWidth, _creditsChartMonths, _creditsChartTypes, _selectedCreditsTypeMode);
    }

    private void InitializeCreditsFilters()
    {
        CreditsFilters.Clear();
        CreditsFilters.Add(new CreditsFilterOptionViewModel("This month", CreditsFilter.ThisMonth));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("Last 3 months", CreditsFilter.Last3Months));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("Last 6 months", CreditsFilter.Last6Months));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("Last year", CreditsFilter.LastYear));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("All", CreditsFilter.All));
        SetCreditsFilter(CreditsFilter.LastYear, rebuild: false);
    }

    private void InitializeCreditsTypeModes()
    {
        CreditsTypeModes.Clear();
        CreditsTypeModes.Add(new CreditsTypeModeOptionViewModel("Stacked", CreditsTypeChartMode.Stacked));
        CreditsTypeModes.Add(new CreditsTypeModeOptionViewModel("Grouped", CreditsTypeChartMode.Grouped));
        SetCreditsTypeMode(CreditsTypeChartMode.Stacked, rebuild: false);
    }

    private void SelectCreditsFilter(object? parameter)
    {
        if (parameter is CreditsFilterOptionViewModel option) { SetCreditsFilter(option.Filter); return; }
        if (parameter is CreditsFilter filter) SetCreditsFilter(filter);
    }

    private void SelectCreditsTypeMode(object? parameter)
    {
        if (parameter is CreditsTypeModeOptionViewModel option) { SetCreditsTypeMode(option.Mode); return; }
        if (parameter is CreditsTypeChartMode mode) SetCreditsTypeMode(mode);
    }

    private void SetCreditsFilter(CreditsFilter filter, bool rebuild = true)
    {
        if (_selectedCreditsFilter == filter && CreditsFilters.Count > 0)
        {
            UpdateCreditsFilterSelection();
            return;
        }
        _selectedCreditsFilter = filter;
        UpdateCreditsFilterSelection();
        UpdateCreditsViewState();
        if (rebuild) ApplyCreditsFilter();
    }

    private void SetCreditsTypeMode(CreditsTypeChartMode mode, bool rebuild = true)
    {
        if (_selectedCreditsTypeMode == mode && CreditsTypeModes.Count > 0)
        {
            UpdateCreditsTypeModeSelection();
            return;
        }
        _selectedCreditsTypeMode = mode;
        UpdateCreditsTypeModeSelection();
        UpdateCreditsViewState();
        if (rebuild) ApplyCreditsFilter();
    }

    private void UpdateCreditsFilterSelection()
    {
        foreach (var option in CreditsFilters)
            option.IsSelected = option.Filter == _selectedCreditsFilter;
    }

    private void UpdateCreditsTypeModeSelection()
    {
        foreach (var option in CreditsTypeModes)
            option.IsSelected = option.Mode == _selectedCreditsTypeMode;
    }

    private void ApplyCreditsFilter()
    {
        RefreshCreditsByMonthChart(FilterCredits(Credits, _selectedCreditsFilter));
    }

    private static IEnumerable<CreditDTO> FilterCredits(IEnumerable<CreditDTO> credits, CreditsFilter filter)
    {
        if (filter == CreditsFilter.All) return credits;

        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var monthsBack = filter switch
        {
            CreditsFilter.ThisMonth => 0,
            CreditsFilter.Last3Months => 2,
            CreditsFilter.Last6Months => 5,
            CreditsFilter.LastYear => 11,
            _ => 0
        };

        var start = currentMonthStart.AddMonths(-monthsBack);
        var endExclusive = currentMonthStart.AddMonths(1);
        return credits.Where(credit => credit.Date >= start && credit.Date < endExclusive);
    }

    private void RefreshCreditsByMonthChart(IEnumerable<CreditDTO> credits)
    {
        CreditsByMonthChart.Clear();
        var grouped = credits
            .GroupBy(credit => new DateTime(credit.Date.Year, credit.Date.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var totals = group
                    .GroupBy(credit => credit.Type, StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(typeGroup => typeGroup.Key, StringComparer.CurrentCultureIgnoreCase)
                    .ToDictionary(
                        typeGroup => typeGroup.Key,
                        typeGroup => typeGroup.Sum(credit => credit.Value),
                        StringComparer.CurrentCultureIgnoreCase);
                return new CreditsMonthTypeTotals(group.Key, totals);
            })
            .ToList();

        _creditsChartMonths = grouped;
        _creditsChartTypes = grouped
            .SelectMany(month => month.TotalsByType.Keys)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(type => type, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var group in grouped)
            CreditsByMonthChart.Add(new KeyValuePair<string, decimal>(group.Month.ToString("MM/yyyy"), group.Total));

        CreditsPlotModel = CreditsChartBuilder.Build(grouped, _creditsChartTypes, _selectedCreditsTypeMode);
        if (CreditsPlotModel != null)
            CreditsChartBuilder.ApplyLabelDensity(CreditsPlotModel, _creditsPlotWidth, _creditsChartMonths, _creditsChartTypes, _selectedCreditsTypeMode);
    }

    private void SetCreditsContext(string contextKey, bool rebuild = true)
    {
        _creditsContextKey = string.IsNullOrWhiteSpace(contextKey) ? DefaultCreditsContextKey : contextKey;
        var state = GetCreditsViewState(_creditsContextKey);
        ApplyCreditsViewState(state, rebuild);
    }

    private CreditsViewState GetCreditsViewState(string contextKey)
    {
        if (_creditsViewStateByKey.TryGetValue(contextKey, out var state))
            return state;
        state = new CreditsViewState(CreditsFilter.LastYear, CreditsTypeChartMode.Stacked);
        _creditsViewStateByKey[contextKey] = state;
        return state;
    }

    private void ApplyCreditsViewState(CreditsViewState state, bool rebuild)
    {
        SetCreditsFilter(state.Filter, rebuild: false);
        SetCreditsTypeMode(state.Mode, rebuild: false);
        if (rebuild) ApplyCreditsFilter();
    }

    private void UpdateCreditsViewState()
    {
        if (!string.IsNullOrWhiteSpace(_creditsContextKey))
            _creditsViewStateByKey[_creditsContextKey] = new CreditsViewState(_selectedCreditsFilter, _selectedCreditsTypeMode);
    }

    private static string BuildAssetKey(string brokerName, string portfolioName, string assetName) =>
        $"{brokerName}|{portfolioName}|{assetName}";

    private static string BuildCreditsAssetKey(string brokerName, string portfolioName, string assetName) =>
        $"Asset|{brokerName}|{portfolioName}|{assetName}";

    private static string BuildPortfolioKey(string brokerName, string portfolioName) =>
        $"Portfolio|{brokerName}|{portfolioName}";

    private static string BuildBrokerKey(string brokerName) => $"Broker|{brokerName}";

    private async void AddTransaction() => await _transactionActions.Add(ShowAddTransactionDialog);
    private async void AddCredit() => await _creditActions.Add(ShowAddCreditDialog);

    private async void UpdateTransaction(object? parameter)
    {
        if (parameter is TransactionDTO tx) SelectedTransaction = tx;
        await _transactionActions.Update(SelectedTransaction, ShowUpdateTransactionDialog);
    }

    private async void UpdateCredit(object? parameter)
    {
        if (parameter is CreditDTO credit) SelectedCredit = credit;
        await _creditActions.Update(SelectedCredit, ShowUpdateCreditDialog);
    }

    private async void DeleteTransaction(object? parameter)
    {
        if (parameter is TransactionDTO tx) SelectedTransaction = tx;
        await _transactionActions.Delete(SelectedTransaction, ShowDeleteTransactionDialog);
    }

    private async void DeleteCredit(object? parameter)
    {
        if (parameter is CreditDTO credit) SelectedCredit = credit;
        await _creditActions.Delete(SelectedCredit, ShowDeleteCreditDialog);
    }

    private void UpdateCommandStates()
    {
        _addTransactionCommand.RaiseCanExecuteChanged();
        _updateTransactionCommand.RaiseCanExecuteChanged();
        _deleteTransactionCommand.RaiseCanExecuteChanged();
        _addCreditCommand.RaiseCanExecuteChanged();
        _updateCreditCommand.RaiseCanExecuteChanged();
        _deleteCreditCommand.RaiseCanExecuteChanged();
        _refreshTodayInfoCommand.RaiseCanExecuteChanged();
        _copyAssetNameCommand.RaiseCanExecuteChanged();
    }

    private bool ShowTransactionDialog(TransactionDialogViewModel viewModel)
    {
        var dialog = new TransactionDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }

    private TransactionDialogData? ShowAddTransactionDialog()
    {
        var vm = TransactionDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowTransactionDialog(vm)) return null;
        return new TransactionDialogData(vm.TransactionId, vm.Date, vm.Type, vm.Quantity, vm.UnitPrice, vm.Fees);
    }

    private TransactionDialogData? ShowUpdateTransactionDialog()
    {
        if (SelectedTransaction == null) return null;
        var vm = TransactionDialogViewModel.CreateForUpdate(
            BrokerName, PortfolioName, AssetName,
            SelectedTransaction.Id, SelectedTransaction.Date, SelectedTransaction.Type,
            SelectedTransaction.Quantity, SelectedTransaction.UnitPrice, SelectedTransaction.Fees);
        if (!ShowTransactionDialog(vm)) return null;
        return new TransactionDialogData(vm.TransactionId, vm.Date, vm.Type, vm.Quantity, vm.UnitPrice, vm.Fees);
    }

    private bool ShowDeleteTransactionDialog()
    {
        if (SelectedTransaction == null) return false;
        var vm = TransactionDialogViewModel.CreateForDelete(
            BrokerName, PortfolioName, AssetName,
            SelectedTransaction.Id, SelectedTransaction.Date, SelectedTransaction.Type,
            SelectedTransaction.Quantity, SelectedTransaction.UnitPrice, SelectedTransaction.Fees);
        return ShowTransactionDialog(vm);
    }

    private bool ShowCreditDialog(CreditDialogViewModel viewModel)
    {
        var dialog = new CreditDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }

    private CreditDialogData? ShowAddCreditDialog()
    {
        var vm = CreditDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowCreditDialog(vm)) return null;
        return new CreditDialogData(vm.CreditId, vm.Date, vm.Type, vm.Value);
    }

    private CreditDialogData? ShowUpdateCreditDialog()
    {
        if (SelectedCredit == null) return null;
        var vm = CreditDialogViewModel.CreateForUpdate(
            BrokerName, PortfolioName, AssetName,
            SelectedCredit.Id, SelectedCredit.Date, SelectedCredit.Type, SelectedCredit.Value);
        if (!ShowCreditDialog(vm)) return null;
        return new CreditDialogData(vm.CreditId, vm.Date, vm.Type, vm.Value);
    }

    private bool ShowDeleteCreditDialog()
    {
        if (SelectedCredit == null) return false;
        var vm = CreditDialogViewModel.CreateForDelete(
            BrokerName, PortfolioName, AssetName,
            SelectedCredit.Id, SelectedCredit.Date, SelectedCredit.Type, SelectedCredit.Value);
        return ShowCreditDialog(vm);
    }
}
