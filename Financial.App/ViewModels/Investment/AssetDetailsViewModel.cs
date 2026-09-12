using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Presentation.App.Helpers;
using OxyPlot;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class AssetDetailsViewModel : ViewModelBase, IAssetDetailsViewModel
{
    private readonly ICreditService _creditService;
    private readonly IAssetPriceLookupService? _priceLookupService;
    private readonly IAssetPriceHistoryService? _priceHistoryService;
    private readonly IAssetPriceService _assetPriceService;
    private readonly IBrokerBreakdownService _brokerBreakdownService;
    private readonly INavigationService _navigationService;
    private readonly IPortfolioAssetSummaryService _portfolioAssetSummaryService;
    private readonly IProfitCalculationService _profitCalculationService;
    private readonly InvestmentScope _scope;
    private readonly TodayInfoTracker _todayInfo;
    private readonly RelayCommand _refreshTodayInfoCommand;
    private readonly RelayCommand _copyAssetNameCommand;
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
    private bool _isAssetView;
    private int _selectedDetailTabIndex;
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
    private string _footerEstimatedAnnualCreditsDisplay = "—";
    private readonly List<(PortfolioAssetSummaryRowViewModel Row, PropertyChangedEventHandler Handler)> _rowSubscriptions = new();
    private IReadOnlyList<AssetCashFlowDTO> _cashFlowsWithCredits = Array.Empty<AssetCashFlowDTO>();
    private IReadOnlyList<AssetCashFlowDTO> _cashFlowsWithoutCredits = Array.Empty<AssetCashFlowDTO>();
    private decimal? _marketValue;
    private decimal _costOfUnitsHeld;
    private decimal? _unrealisedGain;
    private bool _isPriceStale;
    private bool _isPriceUnavailable;
    private decimal? _priceOnlyReturn;
    private decimal? _totalReturn;
    private decimal? _summaryMarketValue;
    private int _summaryHoldingCount;
    private int _summaryUnvaluedHoldingCount;
    private decimal? _summaryPriceOnlyReturn;
    private decimal? _summaryTotalReturn;
    private decimal? _summaryTotalReturnNetOfTax;

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
    public bool IsPriceStale => _isPriceStale;
    public bool IsPriceUnavailable => _isPriceUnavailable;

    public decimal? TotalCurrentValue => _marketValue;
    public decimal? ResultPercent => _unrealisedGain.HasValue && _costOfUnitsHeld != 0 ? _unrealisedGain.Value / _costOfUnitsHeld : null;
    public decimal? TotalCurrentValueWithCredits => _marketValue.HasValue ? _marketValue.Value + TotalCredits : null;

    public decimal? ResultPercentWithCredits =>
        _unrealisedGain.HasValue && _costOfUnitsHeld != 0 ? (_unrealisedGain.Value + TotalCredits) / _costOfUnitsHeld : null;

    public bool HasAveragePrice => _profitCalculationService.HasCostBasis(AveragePrice, Quantity);
    public bool IsActiveScope => _scope == InvestmentScope.Active;
    public bool IsHistoricScope => _scope == InvestmentScope.Historic;
    public decimal? Xirr => _priceOnlyReturn;
    public decimal? XirrWithCredits => _totalReturn;

    // Historic (closed) positions have no live price to mark-to-market: the server solves this
    // against a 0 terminal value (every buy/sell/credit is already a dated entry), which is the
    // same PriceOnlyReturn/TotalReturn field the Active scope reads - Historic's market value is
    // a concrete zero, not unavailable, so the server always populates these.
    public decimal? RealizedXirr => _priceOnlyReturn;
    public decimal? RealizedXirrWithCredits => _totalReturn;

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

    public bool IsAssetView
    {
        get => _isAssetView;
        private set
        {
            var wasAssetView = _isAssetView;
            if (SetProperty(ref _isAssetView, value) && wasAssetView && !value)
            {
                // The Price History tab is only offered for an asset (index 3, see NavigationView.xaml);
                // leaving it selected while it silently hides behind a broker/portfolio node would leave
                // its stale content on screen with no visible tab header pointing at it.
                _selectedDetailTabIndex = 0;
                OnPropertyChanged(nameof(SelectedDetailTabIndex));
            }
        }
    }

    public int SelectedDetailTabIndex => _selectedDetailTabIndex;

    public decimal TotalInvested
    {
        get => _totalInvested;
        private set => SetProperty(ref _totalInvested, value);
    }

    public decimal? MarketValue { get => _summaryMarketValue; private set => SetProperty(ref _summaryMarketValue, value); }
    public int HoldingCount { get => _summaryHoldingCount; private set => SetProperty(ref _summaryHoldingCount, value); }
    public int UnvaluedHoldingCount { get => _summaryUnvaluedHoldingCount; private set => SetProperty(ref _summaryUnvaluedHoldingCount, value); }
    public decimal? PriceOnlyReturn { get => _summaryPriceOnlyReturn; private set => SetProperty(ref _summaryPriceOnlyReturn, value); }
    public decimal? TotalReturn { get => _summaryTotalReturn; private set => SetProperty(ref _summaryTotalReturn, value); }
    public decimal? TotalReturnNetOfTax { get => _summaryTotalReturnNetOfTax; private set => SetProperty(ref _summaryTotalReturnNetOfTax, value); }

    public bool HasIncompleteValuation => UnvaluedHoldingCount > 0;

    public string IncompleteValuationMessage =>
        !HasIncompleteValuation
            ? string.Empty
            : MarketValue is null
                ? $"None of the {HoldingCount} holdings could be valued; returns are withheld."
                : $"{UnvaluedHoldingCount} of {HoldingCount} holdings could not be valued; the total is incomplete and returns are withheld.";

    public bool HasIncompleteShareBasis => IsActiveScope && HasIncompleteValuation;

    public string IncompleteShareBasisMessage =>
        !HasIncompleteShareBasis
            ? string.Empty
            : MarketValue is null
                ? "No portfolio share can be computed for the same reason."
                : "Portfolio shares also do not total 100% for the same reason.";

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

    public TransactionsTabViewModel Transactions { get; }

    public CreditsTabViewModel Credits { get; }

    public PriceHistoryTabViewModel PriceHistory { get; }

    public RelayCommand RefreshTodayInfoCommand => _refreshTodayInfoCommand;
    public RelayCommand CopyAssetNameCommand => _copyAssetNameCommand;

    public AssetDetailsViewModel(
        ITransactionService transactionService,
        ICreditService creditService,
        IAssetPriceService assetPriceService,
        IBrokerBreakdownService brokerBreakdownService,
        ITransactionQueryService transactionQueryService,
        INavigationService navigationService,
        IPortfolioAssetSummaryService portfolioAssetSummaryService,
        IProfitCalculationService profitCalculationService,
        InvestmentScope scope = InvestmentScope.Active,
        IAssetPriceLookupService? priceLookupService = null,
        IAssetPriceHistoryService? priceHistoryService = null)
    {
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
        _priceLookupService = priceLookupService;
        _priceHistoryService = priceHistoryService;
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _brokerBreakdownService = brokerBreakdownService ?? throw new ArgumentNullException(nameof(brokerBreakdownService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _portfolioAssetSummaryService = portfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryService));
        _profitCalculationService = profitCalculationService ?? throw new ArgumentNullException(nameof(profitCalculationService));
        _scope = scope;
        _todayInfo = new TodayInfoTracker(ApplyTodayInfo, ResetTodayInfo, UpdateCommandStates);
        Transactions = new TransactionsTabViewModel(
            transactionService,
            transactionQueryService,
            scope,
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
            _priceHistoryService,
            () => HasAssetContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            details => LoadAssetDetails(details),
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _refreshTodayInfoCommand = new RelayCommand(RefreshTodayInfo, CanRefreshTodayInfo);
        _copyAssetNameCommand = new RelayCommand(CopyAssetName, CanCopyAssetName);
    }

    public void LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems)
    {
        CancelAndResetRowPriceFetch();
        LoadAggregateCredits(BuildPortfolioKey(brokerName, portfolioName), summary, credits);
        IsPortfolioView = true;
        TotalInvested = summary.TotalInvested;

        PortfolioAssetSummaryRows.Clear();
        foreach (var item in assetItems)
            PortfolioAssetSummaryRows.Add(new PortfolioAssetSummaryRowViewModel(item, _profitCalculationService));

        FooterTotalInvested = summary.TotalInvested;
        FooterRealizedGainLoss = assetItems.Sum(i => i.RealizedGainLoss);
        FooterTotalCredits = summary.TotalCredits;
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
        IsAssetView = true;
        CancelAndResetBreakdownFetch();
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
        ApplyValuation(details);

        Transactions.Load(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), details.Transactions);

        Credits.Load(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), details.Credits);

        PriceHistory.Load(BuildCreditsAssetKey(details.BrokerName, details.PortfolioName, details.Name), details.PriceSnapshots, details.Transactions);

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
        IsAssetView = false;
        TotalInvested = 0m;
        MarketValue = null;
        HoldingCount = 0;
        UnvaluedHoldingCount = 0;
        PriceOnlyReturn = null;
        TotalReturn = null;
        TotalReturnNetOfTax = null;
        OnPropertyChanged(nameof(HasIncompleteValuation));
        OnPropertyChanged(nameof(IncompleteValuationMessage));
        OnPropertyChanged(nameof(HasIncompleteShareBasis));
        OnPropertyChanged(nameof(IncompleteShareBasisMessage));
        ClearValuation();
        CancelAndResetBreakdownFetch();
        ClearAssetContext();
        Credits.Clear();
        PriceHistory.Clear();
        HasCreditsContext = false;
        Transactions.Clear();
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

    private bool HasAssetContext =>
        !string.IsNullOrWhiteSpace(BrokerName) &&
        !string.IsNullOrWhiteSpace(PortfolioName) &&
        !string.IsNullOrWhiteSpace(AssetName);

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

    private async Task RefreshTodayInfoAsync(bool forceRefresh)
    {
        if (_scope == InvestmentScope.Historic)
        {
            return;
        }

        var brokerName = BrokerName;
        var portfolioName = PortfolioName;
        var assetName = AssetName;

        var applied = await _todayInfo.RefreshAsync(
            forceRefresh, HasAssetContext, _priceLookupService,
            Class, brokerName,
            Exchange, Ticker, assetName, portfolioName, assetName, message => TodayInfoMessage = message);

        if (!applied)
        {
            return;
        }

        var refreshed = _navigationService.GetAssetDetails(brokerName, portfolioName, assetName, _scope);
        if (refreshed != null)
        {
            ApplyValuation(refreshed);
        }
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
        OnPropertyChanged(nameof(IsPriceStale));
        OnPropertyChanged(nameof(IsPriceUnavailable));
    }

    private void ApplyValuation(AssetDetailsDTO details)
    {
        _marketValue = details.MarketValue;
        _costOfUnitsHeld = details.CostOfUnitsHeld;
        _unrealisedGain = details.UnrealisedGain;
        _isPriceStale = details.MarketStatus == MarketStatus.Stale;
        _isPriceUnavailable = details.MarketStatus == MarketStatus.Unavailable;
        _priceOnlyReturn = details.PriceOnlyReturn;
        _totalReturn = details.TotalReturn;
        NotifyCurrentValueChanged();
    }

    private void ClearValuation()
    {
        _marketValue = null;
        _costOfUnitsHeld = 0m;
        _unrealisedGain = null;
        _isPriceStale = false;
        _isPriceUnavailable = false;
        _priceOnlyReturn = null;
        _totalReturn = null;
        NotifyCurrentValueChanged();
    }

    private void LoadAggregateCredits(string contextKey, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
    {
        IsPortfolioView = false;
        IsBrokerView = false;
        IsAssetView = false;
        CancelAndResetBreakdownFetch();
        ClearAssetContext();
        TotalBought = summary.TotalBought;
        TotalSold = summary.TotalSold;
        HasCreditsContext = true;
        MarketValue = summary.MarketValue;
        HoldingCount = summary.HoldingCount;
        UnvaluedHoldingCount = summary.UnvaluedHoldingCount;
        PriceOnlyReturn = summary.PriceOnlyReturn;
        TotalReturn = summary.TotalReturn;
        TotalReturnNetOfTax = summary.TotalReturnNetOfTax;
        OnPropertyChanged(nameof(HasIncompleteValuation));
        OnPropertyChanged(nameof(IncompleteValuationMessage));
        OnPropertyChanged(nameof(HasIncompleteShareBasis));
        OnPropertyChanged(nameof(IncompleteShareBasisMessage));

        Credits.LoadAggregate(contextKey, credits);
        TotalCredits = credits.Sum(credit => credit.Value);

        Transactions.LoadAggregate(contextKey);

        UpdateCommandStates();
    }

    private void FetchRowPricesAsync(IReadOnlyList<PortfolioAssetSummaryRowViewModel> rows, CancellationToken cancellationToken, string brokerName, string portfolioName)
    {
        var rowTasks = rows.Select(row => FetchRowPriceAsync(row, cancellationToken, brokerName, portfolioName)).ToArray();
        _ = RefreshPortfolioValuationAsync(rowTasks, cancellationToken, brokerName, portfolioName);
    }

    private Task FetchRowPriceAsync(PortfolioAssetSummaryRowViewModel row, CancellationToken cancellationToken, string brokerName, string portfolioName)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (_priceLookupService == null)
                {
                    row.MarkPriceFailed();
                    return;
                }

                var price = await _priceLookupService.GetCurrentPriceAsync(new AssetPriceRequestDTO
                {
                    Exchange = row.Exchange,
                    Ticker = row.Ticker,
                    AssetClass = row.Class,
                    BrokerName = brokerName,
                    Name = row.AssetName,
                    PortfolioName = portfolioName,
                    AssetName = row.AssetName
                });
                if (cancellationToken.IsCancellationRequested) return;
                row.ApplyPrice(price.Price, price.IsManual);
            }
            catch
            {
                if (!cancellationToken.IsCancellationRequested)
                    row.MarkPriceFailed();
            }
        }, cancellationToken);
    }

    // Each row fetch also records its price into the asset's price history server-side, so
    // once every row has settled the grid re-reads the server-computed valuation rather than
    // deriving it here.
    private async Task RefreshPortfolioValuationAsync(Task[] rowTasks, CancellationToken cancellationToken, string brokerName, string portfolioName)
    {
        await Task.WhenAll(rowTasks).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            var refreshed = _portfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName, _scope);
            if (cancellationToken.IsCancellationRequested) return;

            var byName = refreshed.ToDictionary(item => item.AssetName);
            foreach (var row in PortfolioAssetSummaryRows)
            {
                if (byName.TryGetValue(row.AssetName, out var item))
                    row.ApplyValuation(item);
            }
            OnPropertyChanged(nameof(FooterCurrentValueDisplay));
        }
        catch
        {
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
    }

    private static string BuildAssetKey(string brokerName, string portfolioName, string assetName) =>
        $"{brokerName}|{portfolioName}|{assetName}";

    private static string BuildCreditsAssetKey(string brokerName, string portfolioName, string assetName) =>
        $"Asset|{brokerName}|{portfolioName}|{assetName}";

    private static string BuildPortfolioKey(string brokerName, string portfolioName) =>
        $"Portfolio|{brokerName}|{portfolioName}";

    private static string BuildBrokerKey(string brokerName) => $"Broker|{brokerName}";

    private void UpdateCommandStates()
    {
        Transactions.UpdateCommandStates();
        Credits.UpdateCommandStates();
        PriceHistory.UpdateCommandStates();
        _refreshTodayInfoCommand.RaiseCanExecuteChanged();
        _copyAssetNameCommand.RaiseCanExecuteChanged();
    }
}
