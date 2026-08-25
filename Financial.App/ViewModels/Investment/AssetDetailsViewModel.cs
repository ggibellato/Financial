using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Views.Investment;
using Financial.Presentation.App.Helpers;
using OxyPlot;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class AssetDetailsViewModel : ViewModelBase, IAssetDetailsViewModel
{
    private readonly ITransactionService _transactionService;
    private readonly ICreditService _creditService;
    private readonly IPriceService? _priceService;
    private readonly IAssetPriceService _assetPriceService;
    private readonly IBrokerBreakdownService _brokerBreakdownService;
    private readonly ITransactionQueryService _transactionQueryService;
    private readonly IXirrCalculationService _xirrCalculationService;
    private readonly IProfitCalculationService _profitCalculationService;
    private readonly InvestmentScope _scope;
    private readonly TodayInfoTracker _todayInfo;
    private readonly TransactionActions _transactionActions;
    private readonly RelayCommand _addTransactionCommand;
    private readonly RelayCommand _updateTransactionCommand;
    private readonly RelayCommand _deleteTransactionCommand;
    private readonly RelayCommand _refreshTodayInfoCommand;
    private readonly RelayCommand _copyAssetNameCommand;
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
    private decimal? _realizedPortfolioWeight;
    private decimal _todayCurrentValue;
    private string _todayCurrentValueAsOf = string.Empty;
    private string _todayInfoMessage = string.Empty;
    private bool _todayCurrentValueIsManual;
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
    private decimal _footerRealizedGainLoss;
    private decimal _footerTotalCredits;
    private decimal _footerCurrentMonthCredits;
    private string _footerCurrentMonthLabel = string.Empty;
    private TransactionDialogViewModel? _transactionFormViewModel;
    private bool _isTransactionFormOpen;
    private string _footerEstimatedAnnualCreditsDisplay = "—";
    private readonly List<(PortfolioAssetSummaryRowViewModel Row, PropertyChangedEventHandler Handler)> _rowSubscriptions = new();
    private TransactionDTO? _selectedTransaction;
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

    public bool IsCreditsAssetView => HasCreditsContext && !Credits.IsCreditsAggregateView;
    public bool ShouldShowEmptyState => !HasCreditsContext;
    public decimal Balance => TotalBought - TotalSold;

    public decimal TodayCurrentValue
    {
        get => _todayCurrentValue;
        private set { if (SetProperty(ref _todayCurrentValue, value)) NotifyCurrentValueChanged(); }
    }

    public string TodayCurrentValueAsOf { get => _todayCurrentValueAsOf; private set => SetProperty(ref _todayCurrentValueAsOf, value); }
    public string TodayInfoMessage { get => _todayInfoMessage; private set => SetProperty(ref _todayInfoMessage, value); }
    public bool TodayCurrentValueIsManual { get => _todayCurrentValueIsManual; private set => SetProperty(ref _todayCurrentValueIsManual, value); }

    public decimal TotalCurrentValue => TodayCurrentValue * Quantity;
    public decimal ResultPercent => _profitCalculationService.CalculateResultFraction(AveragePrice, Quantity, TotalCurrentValue);
    public decimal TotalCurrentValueWithCredits => TotalCurrentValue + TotalCredits;
    public decimal ResultPercentWithCredits => _profitCalculationService.CalculateResultFraction(AveragePrice, Quantity, TotalCurrentValueWithCredits);
    public bool HasAveragePrice => _profitCalculationService.HasCostBasis(AveragePrice, Quantity);
    public bool IsActiveScope => _scope == InvestmentScope.Active;
    public bool IsHistoricScope => _scope == InvestmentScope.Historic;
    public decimal? Xirr => _xirrCalculationService.Calculate(_cashFlowsWithoutCredits, TotalCurrentValue);

    // The credits-bearing series already carries every credit as a dated positive flow, so the
    // terminal value is the market value alone. Adding TotalCurrentValueWithCredits here would
    // count each credit a second time and flatter the result.
    public decimal? XirrWithCredits => _xirrCalculationService.Calculate(_cashFlowsWithCredits, TotalCurrentValue);

    // Historic (closed) positions have no live price to mark-to-market: XIRR is derived from
    // already-realized cash flows alone, with a 0 terminal value (every buy/sell/credit is
    // already a dated entry), matching the Web app's equivalent calculation.
    public decimal? RealizedXirr => _xirrCalculationService.Calculate(_cashFlowsWithoutCredits, 0m);
    public decimal? RealizedXirrWithCredits => _xirrCalculationService.Calculate(_cashFlowsWithCredits, 0m);

    public decimal? RealizedPortfolioWeight
    {
        get => _realizedPortfolioWeight;
        private set
        {
            if (SetProperty(ref _realizedPortfolioWeight, value))
            {
                OnPropertyChanged(nameof(DisplayRealizedPortfolioWeight));
            }
        }
    }

    public string DisplayRealizedPortfolioWeight =>
        RealizedPortfolioWeight.HasValue ? $"{RealizedPortfolioWeight.Value:F2}%" : "—";

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
    public decimal FooterRealizedGainLoss { get => _footerRealizedGainLoss; private set => SetProperty(ref _footerRealizedGainLoss, value); }
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

    public CreditsTabViewModel Credits { get; }

    public PriceHistoryTabViewModel PriceHistory { get; }

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

    public RelayCommand AddTransactionCommand => _addTransactionCommand;
    public RelayCommand UpdateTransactionCommand => _updateTransactionCommand;
    public RelayCommand DeleteTransactionCommand => _deleteTransactionCommand;
    public RelayCommand RefreshTodayInfoCommand => _refreshTodayInfoCommand;
    public RelayCommand CopyAssetNameCommand => _copyAssetNameCommand;
    public RelayCommand SelectTransactionsFilterCommand => _selectTransactionsFilterCommand;
    public RelayCommand SelectTransactionsChartModeCommand => _selectTransactionsChartModeCommand;

    public TransactionDialogViewModel? TransactionFormViewModel
    {
        get => _transactionFormViewModel;
        private set => SetProperty(ref _transactionFormViewModel, value);
    }

    public bool IsTransactionFormOpen
    {
        get => _isTransactionFormOpen;
        private set => SetProperty(ref _isTransactionFormOpen, value);
    }

    public AssetDetailsViewModel(
        ITransactionService transactionService,
        ICreditService creditService,
        IAssetPriceService assetPriceService,
        IBrokerBreakdownService brokerBreakdownService,
        ITransactionQueryService transactionQueryService,
        IXirrCalculationService xirrCalculationService,
        IProfitCalculationService profitCalculationService,
        InvestmentScope scope = InvestmentScope.Active,
        IPriceService? priceService = null)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
        _priceService = priceService;
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _brokerBreakdownService = brokerBreakdownService ?? throw new ArgumentNullException(nameof(brokerBreakdownService));
        _transactionQueryService = transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService));
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
        _profitCalculationService = profitCalculationService ?? throw new ArgumentNullException(nameof(profitCalculationService));
        _scope = scope;
        _todayInfo = new TodayInfoTracker(ApplyTodayInfo, ResetTodayInfo, UpdateCommandStates);
        _transactionActions = new TransactionActions(
            _transactionService,
            () => HasAssetContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            details => LoadAssetDetails(details),
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        Credits = new CreditsTabViewModel(
            _creditService,
            () => HasAssetContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            details => LoadAssetDetails(details),
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        PriceHistory = new PriceHistoryTabViewModel(
            _priceService,
            () => HasAssetContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            details => LoadAssetDetails(details),
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _addTransactionCommand = new RelayCommand(AddTransaction, CanEditTransactions);
        _updateTransactionCommand = new RelayCommand(UpdateTransaction, CanUpdateTransaction);
        _deleteTransactionCommand = new RelayCommand(DeleteTransaction, CanDeleteTransaction);
        _refreshTodayInfoCommand = new RelayCommand(RefreshTodayInfo, CanRefreshTodayInfo);
        _copyAssetNameCommand = new RelayCommand(CopyAssetName, CanCopyAssetName);
        _selectTransactionsFilterCommand = new RelayCommand(SelectTransactionsFilter);
        _selectTransactionsChartModeCommand = new RelayCommand(SelectTransactionsChartMode);
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
            PortfolioAssetSummaryRows.Add(new PortfolioAssetSummaryRowViewModel(item, _xirrCalculationService, _profitCalculationService));

        FooterTotalInvested = assetItems.Sum(i => i.TotalInvested);
        FooterRealizedGainLoss = assetItems.Sum(i => i.RealizedGainLoss);
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
        if (_scope == InvestmentScope.Historic)
        {
            foreach (var row in rows)
                row.MarkPriceNotApplicable();
            return;
        }

        _rowPriceCts = new CancellationTokenSource();
        var token = _rowPriceCts.Token;
        FetchRowPricesAsync(rows, token, brokerName, portfolioName);
    }

    public void LoadAssetDetails(AssetDetailsDTO details, decimal? realizedPortfolioWeight = null)
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
        RealizedPortfolioWeight = realizedPortfolioWeight;
        HasCreditsContext = true;

        Transactions.Clear();
        foreach (var tx in details.Transactions)
            Transactions.Add(tx);

        Credits.Load(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), details.Credits);

        SetTransactionsContext(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), rebuild: false);
        ApplyTransactionsFilter();

        PriceHistory.Load(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), details.PriceHistory);

        SelectedTransaction = null;
        UpdateCommandStates();
    }

    public void Clear()
    {
        CancelAndResetRowPriceFetch();
        PortfolioAssetSummaryRows.Clear();
        FooterTotalInvested = 0m;
        FooterRealizedGainLoss = 0m;
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
        PriceHistory.Clear();
        HasCreditsContext = false;
        IsTransactionsAggregateView = false;
        CancelAndResetTransactionsFetch();
        _transactionsContextKey = DefaultTransactionsContextKey;
        SelectedTransaction = null;
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
                var breakdown = _brokerBreakdownService.GetBrokerBreakdown(brokerName, _scope);
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
                var transactions = _transactionQueryService.GetTransactionsByBroker(brokerName, _scope);
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
                var transactions = _transactionQueryService.GetTransactionsByPortfolio(brokerName, portfolioName, _scope);
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
        if (_scope == InvestmentScope.Historic)
        {
            return Task.CompletedTask;
        }

        return _todayInfo.RefreshAsync(
            forceRefresh, HasAssetContext, _priceService,
            Class, BrokerName,
            Exchange, Ticker, AssetName, PortfolioName, AssetName, message => TodayInfoMessage = message);
    }

    private void ResetTodayInfo()
    {
        TodayCurrentValue = 0;
        TodayCurrentValueAsOf = string.Empty;
        TodayInfoMessage = string.Empty;
        TodayCurrentValueIsManual = false;
    }

    private void ApplyTodayInfo(TodayInfoSnapshot snapshot)
    {
        TodayCurrentValue = snapshot.Price;
        TodayCurrentValueAsOf = snapshot.AsOf;
        TodayInfoMessage = string.Empty;
        TodayCurrentValueIsManual = snapshot.IsManual;
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
        OnPropertyChanged(nameof(RealizedXirr));
        OnPropertyChanged(nameof(RealizedXirrWithCredits));
    }

    private void LoadAggregateCredits(string contextKey, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        IsPortfolioView = false;
        IsBrokerView = false;
        CancelAndResetBreakdownFetch();
        ClearAssetContext();
        TotalBought = summary.TotalBought;
        TotalSold = summary.TotalSold;
        HasCreditsContext = true;

        Credits.LoadAggregate(contextKey, credits);
        TotalCredits = credits.Sum(credit => credit.Value);

        IsTransactionsAggregateView = true;
        CancelAndResetTransactionsFetch();
        SetTransactionsContext(contextKey, rebuild: false);

        SelectedTransaction = null;
        UpdateCommandStates();
    }

    private void FetchRowPricesAsync(IReadOnlyList<PortfolioAssetSummaryRowViewModel> rows, CancellationToken cancellationToken, string brokerName, string portfolioName)
    {
        foreach (var row in rows)
        {
            var capturedRow = row;
            Task.Run(async () =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (_priceService == null)
                    {
                        capturedRow.MarkPriceFailed();
                        return;
                    }

                    var price = await _priceService.GetCurrentPriceAsync(new AssetPriceRequestDTO
                    {
                        Exchange = capturedRow.Exchange,
                        Ticker = capturedRow.Ticker,
                        AssetClass = capturedRow.Class,
                        BrokerName = brokerName,
                        Name = capturedRow.AssetName,
                        PortfolioName = portfolioName,
                        AssetName = capturedRow.AssetName
                    });
                    if (cancellationToken.IsCancellationRequested) return;
                    capturedRow.ApplyPrice(price.Price, price.IsManual);
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
        RealizedPortfolioWeight = null;
        _cashFlowsWithCredits = Array.Empty<AssetCashFlowDTO>();
        _cashFlowsWithoutCredits = Array.Empty<AssetCashFlowDTO>();
        _todayInfo.Clear();
        Transactions.Clear();
    }

    public void UpdateTransactionsPlotWidth(double plotWidth)
    {
        if (plotWidth <= 0 || TransactionsPlotModel == null) return;
        _transactionsPlotWidth = plotWidth;
        TransactionsChartBuilder.ApplyLabelDensity(TransactionsPlotModel, _transactionsPlotWidth, _transactionsChartMonths);
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

    private async void AddTransaction() => await _transactionActions.Add(ShowAddTransactionFormAsync);

    private async void UpdateTransaction(object? parameter)
    {
        if (parameter is TransactionDTO tx) SelectedTransaction = tx;
        await _transactionActions.Update(SelectedTransaction, ShowUpdateTransactionFormAsync);
    }

    private async void DeleteTransaction(object? parameter)
    {
        if (parameter is TransactionDTO tx) SelectedTransaction = tx;
        await _transactionActions.Delete(SelectedTransaction, ShowDeleteTransactionDialog);
    }

    private void UpdateCommandStates()
    {
        _addTransactionCommand.RaiseCanExecuteChanged();
        _updateTransactionCommand.RaiseCanExecuteChanged();
        _deleteTransactionCommand.RaiseCanExecuteChanged();
        Credits.UpdateCommandStates();
        PriceHistory.UpdateCommandStates();
        _refreshTodayInfoCommand.RaiseCanExecuteChanged();
        _copyAssetNameCommand.RaiseCanExecuteChanged();
    }

    // "New X" / edit actions open an inline form on the same tab instead of a
    // modal dialog (docs/ui/forms-data-and-visualisations.md's "'New X' create
    // actions are inline forms, not popup dialogs" rule) — the form's
    // ConfirmCommand/CancelCommand raise CloseRequested exactly like the old
    // dialog did, so this just awaits that event instead of a blocking
    // ShowDialog() call. Delete stays a real (confirmation) dialog below.
    private Task<TransactionDialogData?> ShowTransactionFormAsync(TransactionDialogViewModel vm)
    {
        var tcs = new TaskCompletionSource<TransactionDialogData?>();
        void OnClosed(object? sender, bool? result)
        {
            vm.CloseRequested -= OnClosed;
            IsTransactionFormOpen = false;
            TransactionFormViewModel = null;
            tcs.SetResult(result == true
                ? new TransactionDialogData(vm.TransactionId, vm.Date, vm.Type, vm.Quantity, vm.UnitPrice, vm.Fees)
                : null);
        }

        vm.CloseRequested += OnClosed;
        TransactionFormViewModel = vm;
        IsTransactionFormOpen = true;
        return tcs.Task;
    }

    private Task<TransactionDialogData?> ShowAddTransactionFormAsync() =>
        ShowTransactionFormAsync(TransactionDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName));

    private Task<TransactionDialogData?> ShowUpdateTransactionFormAsync()
    {
        if (SelectedTransaction == null) return Task.FromResult<TransactionDialogData?>(null);
        var vm = TransactionDialogViewModel.CreateForUpdate(
            BrokerName, PortfolioName, AssetName,
            SelectedTransaction.Id, SelectedTransaction.Date, SelectedTransaction.Type,
            SelectedTransaction.Quantity, SelectedTransaction.UnitPrice, SelectedTransaction.Fees);
        return ShowTransactionFormAsync(vm);
    }

    private bool ShowDeleteTransactionDialog()
    {
        if (SelectedTransaction == null) return false;
        var vm = TransactionDialogViewModel.CreateForDelete(
            BrokerName, PortfolioName, AssetName,
            SelectedTransaction.Id, SelectedTransaction.Date, SelectedTransaction.Type,
            SelectedTransaction.Quantity, SelectedTransaction.UnitPrice, SelectedTransaction.Fees);
        var dialog = new TransactionDialog(vm) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }

}
