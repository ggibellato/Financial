using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Presentation.App.Helpers;
using OxyPlot;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace Financial.Presentation.App.ViewModels;

public class AssetDetailsViewModel : ViewModelBase, IAssetDetailsViewModel
{
    private readonly ITransactionService _transactionService;
    private readonly ICreditService _creditService;
    private readonly IAssetPriceService _assetPriceService;
    private readonly IBrokerBreakdownService _brokerBreakdownService;
    private readonly ITransactionQueryService _transactionQueryService;
    private readonly IXirrCalculationService _xirrCalculationService;
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
    private readonly RelayCommand _selectCreditsChartTypeCommand;
    private readonly RelayCommand _selectTransactionsFilterCommand;
    private readonly RelayCommand _selectTransactionsChartModeCommand;
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
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalCredits;
    private decimal _realizedGainLoss;
    private decimal _todayCurrentValue;
    private string _todayCurrentValueAsOf = string.Empty;
    private string _todayInfoMessage = string.Empty;
    private PlotModel? _creditsPlotModel;
    private PeriodFilter _selectedCreditsFilter = PeriodFilter.Last12Months;
    private CreditsTypeChartMode _selectedCreditsTypeMode = CreditsTypeChartMode.Stacked;
    private CreditsChartType _selectedCreditsChartType = CreditsChartType.Bar;
    private const string DefaultCreditsContextKey = "default";
    private readonly Dictionary<string, CreditsViewState> _creditsViewStateByKey = new(StringComparer.OrdinalIgnoreCase);
    private string _creditsContextKey = DefaultCreditsContextKey;
    private double _creditsPlotWidth;
    private bool _isCreditsAggregateView;
    private bool _hasCreditsContext;
    private bool _isPortfolioView;
    private bool _isBrokerView;
    private decimal _totalInvested;
    private PlotModel? _overallBreakdownPlotModel;
    private bool _isBreakdownLoading;
    private string? _breakdownError;
    private CancellationTokenSource? _breakdownCts;
    private CancellationTokenSource? _rowPriceCts;
    private decimal _footerTotalInvested;
    private decimal _footerTotalCredits;
    private decimal _footerCurrentMonthCredits;
    private string _footerCurrentMonthLabel = string.Empty;
    private string _footerEstimatedAnnualCreditsDisplay = "—";
    private readonly List<(PortfolioAssetSummaryRowViewModel Row, PropertyChangedEventHandler Handler)> _rowSubscriptions = new();
    private TransactionDTO? _selectedTransaction;
    private CreditDTO? _selectedCredit;
    private IReadOnlyList<CreditsMonthTypeTotals> _creditsChartMonths = Array.Empty<CreditsMonthTypeTotals>();
    private IReadOnlyList<string> _creditsChartTypes = Array.Empty<string>();
    private PlotModel? _transactionsPlotModel;
    private PeriodFilter _selectedTransactionsFilter = PeriodFilter.Last12Months;
    private ChartTypeMode _selectedTransactionsChartMode = ChartTypeMode.Bar;
    private const string DefaultTransactionsContextKey = "default";
    private readonly Dictionary<string, TransactionsViewState> _transactionsViewStateByKey = new(StringComparer.OrdinalIgnoreCase);
    private string _transactionsContextKey = DefaultTransactionsContextKey;
    private double _transactionsPlotWidth;
    private bool _isTransactionsAggregateView;
    private bool _isTransactionsLoading;
    private string? _transactionsError;
    private CancellationTokenSource? _transactionsCts;
    private IReadOnlyList<TransactionSummaryItemDTO> _brokerPortfolioTransactions = Array.Empty<TransactionSummaryItemDTO>();
    private IReadOnlyList<TransactionMonthNet> _transactionsChartMonths = Array.Empty<TransactionMonthNet>();
    private IReadOnlyList<AssetCashFlowDTO> _cashFlowsWithCredits = Array.Empty<AssetCashFlowDTO>();
    private IReadOnlyList<AssetCashFlowDTO> _cashFlowsWithoutCredits = Array.Empty<AssetCashFlowDTO>();

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

    public decimal RealizedGainLoss
    {
        get => _realizedGainLoss;
        private set => SetProperty(ref _realizedGainLoss, value);
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
    public decimal? Xirr => _xirrCalculationService.Calculate(_cashFlowsWithoutCredits, TotalCurrentValue);
    public decimal? XirrWithCredits => _xirrCalculationService.Calculate(_cashFlowsWithCredits, TotalCurrentValueWithCredits);

    public bool IsPortfolioView
    {
        get => _isPortfolioView;
        private set => SetProperty(ref _isPortfolioView, value);
    }

    public bool IsBrokerView
    {
        get => _isBrokerView;
        private set => SetProperty(ref _isBrokerView, value);
    }

    public decimal TotalInvested
    {
        get => _totalInvested;
        private set => SetProperty(ref _totalInvested, value);
    }

    public PlotModel? OverallBreakdownPlotModel
    {
        get => _overallBreakdownPlotModel;
        private set => SetProperty(ref _overallBreakdownPlotModel, value);
    }

    public bool IsBreakdownLoading
    {
        get => _isBreakdownLoading;
        private set
        {
            if (SetProperty(ref _isBreakdownLoading, value))
            {
                OnPropertyChanged(nameof(ShowBreakdownEmptyState));
                OnPropertyChanged(nameof(HasBreakdownData));
            }
        }
    }

    public string? BreakdownError
    {
        get => _breakdownError;
        private set
        {
            if (SetProperty(ref _breakdownError, value))
            {
                OnPropertyChanged(nameof(HasBreakdownError));
                OnPropertyChanged(nameof(ShowBreakdownEmptyState));
                OnPropertyChanged(nameof(HasBreakdownData));
            }
        }
    }

    public bool HasBreakdownError => BreakdownError != null;
    public bool ShowBreakdownEmptyState => !IsBreakdownLoading && BreakdownError == null && PortfolioBreakdownPieItems.Count == 0;
    public bool HasBreakdownData => !IsBreakdownLoading && BreakdownError == null && PortfolioBreakdownPieItems.Count > 0;

    public ObservableCollection<PortfolioBreakdownPieItem> PortfolioBreakdownPieItems { get; } = new();

    public ObservableCollection<PortfolioAssetSummaryRowViewModel> PortfolioAssetSummaryRows { get; } = new();

    public decimal FooterTotalInvested { get => _footerTotalInvested; private set => SetProperty(ref _footerTotalInvested, value); }
    public decimal FooterTotalCredits { get => _footerTotalCredits; private set => SetProperty(ref _footerTotalCredits, value); }
    public decimal FooterCurrentMonthCredits { get => _footerCurrentMonthCredits; private set => SetProperty(ref _footerCurrentMonthCredits, value); }
    public string FooterCurrentMonthLabel { get => _footerCurrentMonthLabel; private set => SetProperty(ref _footerCurrentMonthLabel, value); }
    public string FooterEstimatedAnnualCreditsDisplay { get => _footerEstimatedAnnualCreditsDisplay; private set => SetProperty(ref _footerEstimatedAnnualCreditsDisplay, value); }

    public string FooterCurrentValueDisplay
    {
        get
        {
            if (!PortfolioAssetSummaryRows.Any() || PortfolioAssetSummaryRows.Any(r => r.IsLoadingPrice))
                return "Calculating…";
            return PortfolioAssetSummaryRows.Sum(r => r.CurrentValue ?? 0m).ToString("N2");
        }
    }

    public ObservableCollection<TransactionDTO> Transactions { get; } = new();
    public ObservableCollection<CreditDTO> Credits { get; } = new();
    public ObservableCollection<KeyValuePair<string, decimal>> CreditsByMonthChart { get; } = new();
    public ObservableCollection<CreditsFilterOptionViewModel> CreditsFilters { get; } = new();
    public ObservableCollection<CreditsTypeModeOptionViewModel> CreditsTypeModes { get; } = new();
    public ObservableCollection<CreditsChartTypeOptionViewModel> CreditsChartTypes { get; } = new();

    public PlotModel? TransactionsPlotModel { get => _transactionsPlotModel; private set => SetProperty(ref _transactionsPlotModel, value); }

    public bool IsTransactionsAggregateView
    {
        get => _isTransactionsAggregateView;
        private set => SetProperty(ref _isTransactionsAggregateView, value);
    }

    public bool IsTransactionsLoading
    {
        get => _isTransactionsLoading;
        private set => SetProperty(ref _isTransactionsLoading, value);
    }

    public string? TransactionsError
    {
        get => _transactionsError;
        private set { if (SetProperty(ref _transactionsError, value)) OnPropertyChanged(nameof(HasTransactionsError)); }
    }

    public bool HasTransactionsError => TransactionsError != null;

    public ObservableCollection<TransactionsFilterOptionViewModel> TransactionsFilters { get; } = new();
    public ObservableCollection<ChartTypeModeOptionViewModel> ChartTypeModes { get; } = new();

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
    public RelayCommand SelectCreditsChartTypeCommand => _selectCreditsChartTypeCommand;
    public RelayCommand SelectTransactionsFilterCommand => _selectTransactionsFilterCommand;
    public RelayCommand SelectTransactionsChartModeCommand => _selectTransactionsChartModeCommand;

    public AssetDetailsViewModel(
        ITransactionService transactionService,
        ICreditService creditService,
        IAssetPriceService assetPriceService,
        IBrokerBreakdownService brokerBreakdownService,
        ITransactionQueryService transactionQueryService,
        IXirrCalculationService xirrCalculationService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _brokerBreakdownService = brokerBreakdownService ?? throw new ArgumentNullException(nameof(brokerBreakdownService));
        _transactionQueryService = transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService));
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
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
        _selectCreditsChartTypeCommand = new RelayCommand(SelectCreditsChartType);
        _selectTransactionsFilterCommand = new RelayCommand(SelectTransactionsFilter);
        _selectTransactionsChartModeCommand = new RelayCommand(SelectTransactionsChartMode);
        InitializeCreditsFilters();
        InitializeCreditsTypeModes();
        InitializeCreditsChartTypes();
        InitializeTransactionsFilters();
        InitializeChartTypeModes();
    }

    public void LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems)
    {
        CancelAndResetRowPriceFetch();
        LoadAggregateCredits(BuildPortfolioKey(brokerName, portfolioName), summary, credits);
        IsPortfolioView = true;
        TotalInvested = summary.TotalInvested;

        PortfolioAssetSummaryRows.Clear();
        foreach (var item in assetItems)
            PortfolioAssetSummaryRows.Add(new PortfolioAssetSummaryRowViewModel(item, _xirrCalculationService));

        FooterTotalInvested = assetItems.Sum(i => i.TotalInvested);
        FooterTotalCredits = assetItems.Sum(i => i.TotalCredits);
        FooterCurrentMonthCredits = assetItems.Sum(i => i.CurrentMonthCredits);
        FooterCurrentMonthLabel = "Credits " + DateTime.Today.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        var withEstimated = assetItems.Where(i => i.EstimatedAnnualCredits.HasValue).ToList();
        FooterEstimatedAnnualCreditsDisplay = withEstimated.Any()
            ? withEstimated.Sum(i => i.EstimatedAnnualCredits!.Value).ToString("N2")
            : "—";

        foreach (var row in PortfolioAssetSummaryRows)
            SubscribeToRowPriceChanges(row);
        OnPropertyChanged(nameof(FooterCurrentValueDisplay));

        var rows = PortfolioAssetSummaryRows.ToList();
        _rowPriceCts = new CancellationTokenSource();
        var token = _rowPriceCts.Token;
        FetchRowPricesAsync(rows, token, brokerName);
    }

    public void LoadAssetDetails(AssetDetailsDTO details)
    {
        IsPortfolioView = false;
        IsBrokerView = false;
        CancelAndResetBreakdownFetch();
        IsTransactionsAggregateView = false;
        CancelAndResetTransactionsFetch();
        var assetKey = BuildAssetKey(details.BrokerName, details.PortfolioName, details.Name);
        _todayInfo.UpdateAssetKey(assetKey);
        _cashFlowsWithCredits = details.CashFlowsWithCredits;
        _cashFlowsWithoutCredits = details.CashFlowsWithoutCredits;

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
        TotalBought = details.TotalBought;
        TotalSold = details.TotalSold;
        TotalCredits = details.TotalCredits;
        RealizedGainLoss = details.RealizedGainLoss;
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

        SetTransactionsContext(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), rebuild: false);
        ApplyTransactionsFilter();

        SelectedTransaction = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    public void Clear()
    {
        CancelAndResetRowPriceFetch();
        PortfolioAssetSummaryRows.Clear();
        FooterTotalInvested = 0m;
        FooterTotalCredits = 0m;
        FooterCurrentMonthCredits = 0m;
        FooterCurrentMonthLabel = string.Empty;
        FooterEstimatedAnnualCreditsDisplay = "—";
        OnPropertyChanged(nameof(FooterCurrentValueDisplay));
        IsPortfolioView = false;
        IsBrokerView = false;
        TotalInvested = 0m;
        CancelAndResetBreakdownFetch();
        ClearAssetContext();
        Credits.Clear();
        CreditsByMonthChart.Clear();
        CreditsPlotModel = null;
        IsCreditsAggregateView = false;
        HasCreditsContext = false;
        _creditsContextKey = DefaultCreditsContextKey;
        IsTransactionsAggregateView = false;
        CancelAndResetTransactionsFetch();
        _transactionsContextKey = DefaultTransactionsContextKey;
        SelectedTransaction = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    public void LoadBrokerSummary(string brokerName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        LoadAggregateCredits(BuildBrokerKey(brokerName), summary, credits);
        IsBrokerView = true;
        TotalInvested = summary.TotalInvested;
    }

    public Task LoadBrokerBreakdown(string brokerName)
    {
        CancelAndResetBreakdownFetch();
        IsBreakdownLoading = true;

        _breakdownCts = new CancellationTokenSource();
        var token = _breakdownCts.Token;
        return Task.Run(() =>
        {
            try
            {
                var breakdown = _brokerBreakdownService.GetBrokerBreakdown(brokerName, InvestmentScope.Active);
                if (token.IsCancellationRequested) return;
                ApplyBrokerBreakdown(breakdown);
            }
            catch
            {
                if (token.IsCancellationRequested) return;
                BreakdownError = "Unable to load breakdown";
                IsBreakdownLoading = false;
            }
        }, token);
    }

    public void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        LoadAggregateCredits(BuildPortfolioKey(brokerName, portfolioName), summary, credits);
    }

    public Task LoadBrokerTransactions(string brokerName)
    {
        CancelAndResetTransactionsFetch();
        IsTransactionsLoading = true;

        _transactionsCts = new CancellationTokenSource();
        var token = _transactionsCts.Token;
        return Task.Run(() =>
        {
            try
            {
                var transactions = _transactionQueryService.GetTransactionsByBroker(brokerName);
                if (token.IsCancellationRequested) return;
                ApplyFetchedTransactions(transactions);
            }
            catch
            {
                if (token.IsCancellationRequested) return;
                TransactionsError = "Unable to load transactions";
                IsTransactionsLoading = false;
            }
        }, token);
    }

    public Task LoadPortfolioTransactions(string brokerName, string portfolioName)
    {
        CancelAndResetTransactionsFetch();
        IsTransactionsLoading = true;

        _transactionsCts = new CancellationTokenSource();
        var token = _transactionsCts.Token;
        return Task.Run(() =>
        {
            try
            {
                var transactions = _transactionQueryService.GetTransactionsByPortfolio(brokerName, portfolioName);
                if (token.IsCancellationRequested) return;
                ApplyFetchedTransactions(transactions);
            }
            catch
            {
                if (token.IsCancellationRequested) return;
                TransactionsError = "Unable to load transactions";
                IsTransactionsLoading = false;
            }
        }, token);
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
            Class, BrokerName,
            Exchange, Ticker, AssetName, message => TodayInfoMessage = message);
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
        OnPropertyChanged(nameof(Xirr));
        OnPropertyChanged(nameof(XirrWithCredits));
    }

    private void LoadAggregateCredits(string contextKey, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        IsPortfolioView = false;
        IsBrokerView = false;
        CancelAndResetBreakdownFetch();
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

        IsTransactionsAggregateView = true;
        CancelAndResetTransactionsFetch();
        SetTransactionsContext(contextKey, rebuild: false);

        SelectedTransaction = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    private void FetchRowPricesAsync(IReadOnlyList<PortfolioAssetSummaryRowViewModel> rows, CancellationToken cancellationToken, string brokerName)
    {
        foreach (var row in rows)
        {
            var capturedRow = row;
            Task.Run(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    var price = _assetPriceService.GetCurrentPrice(new Application.DTOs.AssetPriceRequestDTO
                    {
                        Exchange = capturedRow.Exchange,
                        Ticker = capturedRow.Ticker,
                        AssetClass = capturedRow.Class,
                        BrokerName = brokerName,
                        Name = capturedRow.AssetName
                    });
                    if (cancellationToken.IsCancellationRequested) return;
                    capturedRow.ApplyPrice(price.Price);
                }
                catch
                {
                    if (!cancellationToken.IsCancellationRequested)
                        capturedRow.MarkPriceFailed();
                }
            }, cancellationToken);
        }
    }

    private void CancelAndResetRowPriceFetch()
    {
        UnsubscribeFromRowPriceChanges();
        _rowPriceCts?.Cancel();
        _rowPriceCts?.Dispose();
        _rowPriceCts = null;
    }

    private void ApplyBrokerBreakdown(IReadOnlyList<PortfolioBreakdownItemDTO> breakdown)
    {
        var overallModel = BrokerBreakdownChartBuilder.Build(
            breakdown.Select(p => (p.PortfolioName, p.TotalInvested)).ToList());

        var items = breakdown
            .Select(portfolio =>
            {
                var plotModel = BrokerBreakdownChartBuilder.Build(
                    portfolio.Assets.Select(a => (a.AssetName, a.TotalInvested)).ToList());
                return new PortfolioBreakdownPieItem(portfolio.PortfolioName, plotModel);
            })
            .ToList();

        // ObservableCollection structural changes (unlike plain property changes) must
        // happen on the thread that owns the bound CollectionView, or WPF throws
        // NotSupportedException — this method runs on a background thread (Task.Run).
        RunOnUIThread(() =>
        {
            PortfolioBreakdownPieItems.Clear();
            foreach (var item in items)
                PortfolioBreakdownPieItems.Add(item);
        });

        OverallBreakdownPlotModel = overallModel;
        IsBreakdownLoading = false;
    }

    private static void RunOnUIThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }

    private void CancelAndResetBreakdownFetch()
    {
        _breakdownCts?.Cancel();
        _breakdownCts?.Dispose();
        _breakdownCts = null;
        IsBreakdownLoading = false;
        BreakdownError = null;
        OverallBreakdownPlotModel = null;
        PortfolioBreakdownPieItems.Clear();
        OnPropertyChanged(nameof(ShowBreakdownEmptyState));
        OnPropertyChanged(nameof(HasBreakdownData));
    }

    private void ApplyFetchedTransactions(IReadOnlyList<TransactionSummaryItemDTO> transactions)
    {
        _brokerPortfolioTransactions = transactions;
        ApplyTransactionsFilter();
        IsTransactionsLoading = false;
    }

    private void CancelAndResetTransactionsFetch()
    {
        _transactionsCts?.Cancel();
        _transactionsCts?.Dispose();
        _transactionsCts = null;
        IsTransactionsLoading = false;
        TransactionsError = null;
        TransactionsPlotModel = null;
        _brokerPortfolioTransactions = Array.Empty<TransactionSummaryItemDTO>();
        _transactionsChartMonths = Array.Empty<TransactionMonthNet>();
    }

    private void SubscribeToRowPriceChanges(PortfolioAssetSummaryRowViewModel row)
    {
        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PortfolioAssetSummaryRowViewModel.IsLoadingPrice)
                or nameof(PortfolioAssetSummaryRowViewModel.CurrentValue))
                OnPropertyChanged(nameof(FooterCurrentValueDisplay));
        }
        row.PropertyChanged += Handler;
        _rowSubscriptions.Add((row, Handler));
    }

    private void UnsubscribeFromRowPriceChanges()
    {
        foreach (var (row, handler) in _rowSubscriptions)
            row.PropertyChanged -= handler;
        _rowSubscriptions.Clear();
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
        TotalBought = 0;
        TotalSold = 0;
        TotalCredits = 0;
        RealizedGainLoss = 0;
        _cashFlowsWithCredits = Array.Empty<AssetCashFlowDTO>();
        _cashFlowsWithoutCredits = Array.Empty<AssetCashFlowDTO>();
        _todayInfo.Clear();
        Transactions.Clear();
    }

    public void UpdateCreditsPlotWidth(double plotWidth)
    {
        if (plotWidth <= 0 || CreditsPlotModel == null) return;
        _creditsPlotWidth = plotWidth;
        CreditsChartBuilder.ApplyLabelDensity(CreditsPlotModel, _creditsPlotWidth, _creditsChartMonths, _creditsChartTypes, _selectedCreditsTypeMode, _selectedCreditsChartType);
    }

    public void UpdateTransactionsPlotWidth(double plotWidth)
    {
        if (plotWidth <= 0 || TransactionsPlotModel == null) return;
        _transactionsPlotWidth = plotWidth;
        TransactionsChartBuilder.ApplyLabelDensity(TransactionsPlotModel, _transactionsPlotWidth, _transactionsChartMonths);
    }

    private void InitializeCreditsFilters()
    {
        CreditsFilters.Clear();
        foreach (var (label, filter) in PeriodFilterHelper.Options)
            CreditsFilters.Add(new CreditsFilterOptionViewModel(label, filter));
        SetCreditsFilter(PeriodFilter.Last12Months, rebuild: false);
    }

    private void InitializeCreditsTypeModes()
    {
        CreditsTypeModes.Clear();
        CreditsTypeModes.Add(new CreditsTypeModeOptionViewModel("Stacked", CreditsTypeChartMode.Stacked));
        CreditsTypeModes.Add(new CreditsTypeModeOptionViewModel("Grouped", CreditsTypeChartMode.Grouped));
        SetCreditsTypeMode(CreditsTypeChartMode.Stacked, rebuild: false);
    }

    private void InitializeCreditsChartTypes()
    {
        CreditsChartTypes.Clear();
        CreditsChartTypes.Add(new CreditsChartTypeOptionViewModel("Bar", CreditsChartType.Bar));
        CreditsChartTypes.Add(new CreditsChartTypeOptionViewModel("Line", CreditsChartType.Line));
        SetCreditsChartType(CreditsChartType.Bar, rebuild: false);
    }

    private void SelectCreditsFilter(object? parameter)
    {
        if (parameter is CreditsFilterOptionViewModel option) { SetCreditsFilter(option.Filter); return; }
        if (parameter is PeriodFilter filter) SetCreditsFilter(filter);
    }

    private void SelectCreditsTypeMode(object? parameter)
    {
        if (parameter is CreditsTypeModeOptionViewModel option) { SetCreditsTypeMode(option.Mode); return; }
        if (parameter is CreditsTypeChartMode mode) SetCreditsTypeMode(mode);
    }

    private void SelectCreditsChartType(object? parameter)
    {
        if (parameter is CreditsChartTypeOptionViewModel option) { SetCreditsChartType(option.ChartType); return; }
        if (parameter is CreditsChartType chartType) SetCreditsChartType(chartType);
    }

    private void SetCreditsFilter(PeriodFilter filter, bool rebuild = true)
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

    private void SetCreditsChartType(CreditsChartType chartType, bool rebuild = true)
    {
        if (_selectedCreditsChartType == chartType && CreditsChartTypes.Count > 0)
        {
            UpdateCreditsChartTypeSelection();
            return;
        }
        _selectedCreditsChartType = chartType;
        UpdateCreditsChartTypeSelection();
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

    private void UpdateCreditsChartTypeSelection()
    {
        foreach (var option in CreditsChartTypes)
            option.IsSelected = option.ChartType == _selectedCreditsChartType;
    }

    private void ApplyCreditsFilter()
    {
        RefreshCreditsByMonthChart(FilterCredits(Credits, _selectedCreditsFilter));
    }

    private static IEnumerable<CreditDTO> FilterCredits(IEnumerable<CreditDTO> credits, PeriodFilter filter)
    {
        var (start, endExclusive) = PeriodFilterHelper.GetDateRange(filter, DateTime.Today);
        if (start is null) return credits;
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

        CreditsPlotModel = CreditsChartBuilder.Build(grouped, _creditsChartTypes, _selectedCreditsTypeMode, _selectedCreditsChartType);
        if (CreditsPlotModel != null)
            CreditsChartBuilder.ApplyLabelDensity(CreditsPlotModel, _creditsPlotWidth, _creditsChartMonths, _creditsChartTypes, _selectedCreditsTypeMode, _selectedCreditsChartType);
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
        state = new CreditsViewState(PeriodFilter.Last12Months, CreditsTypeChartMode.Stacked, CreditsChartType.Bar);
        _creditsViewStateByKey[contextKey] = state;
        return state;
    }

    private void ApplyCreditsViewState(CreditsViewState state, bool rebuild)
    {
        SetCreditsFilter(state.Filter, rebuild: false);
        SetCreditsTypeMode(state.Mode, rebuild: false);
        SetCreditsChartType(state.ChartType, rebuild: false);
        if (rebuild) ApplyCreditsFilter();
    }

    private void UpdateCreditsViewState()
    {
        if (!string.IsNullOrWhiteSpace(_creditsContextKey))
            _creditsViewStateByKey[_creditsContextKey] = new CreditsViewState(_selectedCreditsFilter, _selectedCreditsTypeMode, _selectedCreditsChartType);
    }

    private void InitializeTransactionsFilters()
    {
        TransactionsFilters.Clear();
        foreach (var (label, filter) in PeriodFilterHelper.Options)
            TransactionsFilters.Add(new TransactionsFilterOptionViewModel(label, filter));
        SetTransactionsFilter(PeriodFilter.Last12Months, rebuild: false);
    }

    private void InitializeChartTypeModes()
    {
        ChartTypeModes.Clear();
        ChartTypeModes.Add(new ChartTypeModeOptionViewModel("Bar", ChartTypeMode.Bar));
        ChartTypeModes.Add(new ChartTypeModeOptionViewModel("Line", ChartTypeMode.Line));
        SetTransactionsChartMode(ChartTypeMode.Bar, rebuild: false);
    }

    private void SelectTransactionsFilter(object? parameter)
    {
        if (parameter is TransactionsFilterOptionViewModel option) { SetTransactionsFilter(option.Filter); return; }
        if (parameter is PeriodFilter filter) SetTransactionsFilter(filter);
    }

    private void SelectTransactionsChartMode(object? parameter)
    {
        if (parameter is ChartTypeModeOptionViewModel option) { SetTransactionsChartMode(option.Mode); return; }
        if (parameter is ChartTypeMode mode) SetTransactionsChartMode(mode);
    }

    private void SetTransactionsFilter(PeriodFilter filter, bool rebuild = true)
    {
        if (_selectedTransactionsFilter == filter && TransactionsFilters.Count > 0)
        {
            UpdateTransactionsFilterSelection();
            return;
        }
        _selectedTransactionsFilter = filter;
        UpdateTransactionsFilterSelection();
        UpdateTransactionsViewState();
        if (rebuild) ApplyTransactionsFilter();
    }

    private void SetTransactionsChartMode(ChartTypeMode mode, bool rebuild = true)
    {
        if (_selectedTransactionsChartMode == mode && ChartTypeModes.Count > 0)
        {
            UpdateTransactionsChartModeSelection();
            return;
        }
        _selectedTransactionsChartMode = mode;
        UpdateTransactionsChartModeSelection();
        UpdateTransactionsViewState();
        if (rebuild) ApplyTransactionsFilter();
    }

    private void UpdateTransactionsFilterSelection()
    {
        foreach (var option in TransactionsFilters)
            option.IsSelected = option.Filter == _selectedTransactionsFilter;
    }

    private void UpdateTransactionsChartModeSelection()
    {
        foreach (var option in ChartTypeModes)
            option.IsSelected = option.Mode == _selectedTransactionsChartMode;
    }

    private void ApplyTransactionsFilter()
    {
        IEnumerable<(DateTime Date, string Type, decimal TotalPrice)> source = IsTransactionsAggregateView
            ? _brokerPortfolioTransactions.Select(t => (t.Date, t.Type, t.TotalPrice))
            : Transactions.Select(t => (t.Date, t.Type, t.TotalPrice));

        var months = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(source, _selectedTransactionsFilter, DateTime.Today);
        _transactionsChartMonths = months;
        TransactionsPlotModel = TransactionsChartBuilder.Build(months, _selectedTransactionsChartMode);
        if (TransactionsPlotModel != null)
            TransactionsChartBuilder.ApplyLabelDensity(TransactionsPlotModel, _transactionsPlotWidth, months);
    }

    private void SetTransactionsContext(string contextKey, bool rebuild = true)
    {
        _transactionsContextKey = string.IsNullOrWhiteSpace(contextKey) ? DefaultTransactionsContextKey : contextKey;
        var state = GetTransactionsViewState(_transactionsContextKey);
        ApplyTransactionsViewState(state, rebuild);
    }

    private TransactionsViewState GetTransactionsViewState(string contextKey)
    {
        if (_transactionsViewStateByKey.TryGetValue(contextKey, out var state))
            return state;
        state = new TransactionsViewState(PeriodFilter.Last12Months, ChartTypeMode.Bar);
        _transactionsViewStateByKey[contextKey] = state;
        return state;
    }

    private void ApplyTransactionsViewState(TransactionsViewState state, bool rebuild)
    {
        SetTransactionsFilter(state.Filter, rebuild: false);
        SetTransactionsChartMode(state.Mode, rebuild: false);
        if (rebuild) ApplyTransactionsFilter();
    }

    private void UpdateTransactionsViewState()
    {
        if (!string.IsNullOrWhiteSpace(_transactionsContextKey))
            _transactionsViewStateByKey[_transactionsContextKey] = new TransactionsViewState(_selectedTransactionsFilter, _selectedTransactionsChartMode);
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
