using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DashboardKpiTilesViewModel : ViewModelBase
{
    private static readonly string[] SummaryProperties =
    [
        nameof(MarketValue),
        nameof(Invested),
        nameof(UnrealisedGainLoss),
        nameof(RealisedGainLoss),
        nameof(IncomeYtd),
        nameof(IncomeLifetime),
        nameof(GrossXirr),
        nameof(NetXirr),
        nameof(ConvertedMarketValue),
        nameof(ConvertedInvested),
        nameof(ConvertedUnrealisedGainLoss),
        nameof(ConvertedRealisedGainLoss),
        nameof(ConvertedIncomeYtd),
        nameof(ConvertedIncomeLifetime),
        nameof(ConvertedGrossXirr),
        nameof(ConvertedNetXirr),
        nameof(ReportingCurrency),
        nameof(IsReportingCurrencyEnabled),
        nameof(IsReportingCurrencyUnavailable),
        nameof(IsReportingCurrencyPartial),
        nameof(ShowConvertedTotals),
        nameof(IsPartial),
        nameof(UnvaluedHoldingCount),
        nameof(PartialNoticeText),
        nameof(ShowPartialNotice),
    ];

    private readonly IPortfolioDashboardService _dashboardService;
    private readonly ILogger<DashboardKpiTilesViewModel> _logger;

    private PortfolioDashboardDTO? _summary;
    private bool _isLoading;
    private string? _errorMessage;
    private int _requestId;

    public DashboardKpiTilesViewModel(
        IPortfolioDashboardService dashboardService,
        ILogger<DashboardKpiTilesViewModel> logger)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        ViewMissingPriceHoldingsCommand = new RelayCommand(() => ExpandMissingPriceRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? ExpandMissingPriceRequested;

    public RelayCommand RefreshCommand { get; }

    public RelayCommand ViewMissingPriceHoldingsCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public bool HasError => ErrorMessage != null;

    public bool ShowContent => !IsLoading && !HasError;

    public decimal? MarketValue => _summary?.MarketValue;

    public decimal? Invested => _summary?.Invested;

    public decimal? UnrealisedGainLoss => _summary?.UnrealisedGainLoss;

    public decimal? RealisedGainLoss => _summary?.RealisedGainLoss;

    public decimal? IncomeYtd => _summary?.IncomeYtd;

    public decimal? IncomeLifetime => _summary?.IncomeLifetime;

    public decimal? GrossXirr => _summary?.GrossXirr;

    public decimal? NetXirr => _summary?.NetXirr;

    public decimal? ConvertedMarketValue => _summary?.ConvertedMarketValue;

    public decimal? ConvertedInvested => _summary?.ConvertedInvested;

    public decimal? ConvertedUnrealisedGainLoss => _summary?.ConvertedUnrealisedGainLoss;

    public decimal? ConvertedRealisedGainLoss => _summary?.ConvertedRealisedGainLoss;

    public decimal? ConvertedIncomeYtd => _summary?.ConvertedIncomeYtd;

    public decimal? ConvertedIncomeLifetime => _summary?.ConvertedIncomeLifetime;

    public decimal? ConvertedGrossXirr => _summary?.ConvertedGrossXirr;

    public decimal? ConvertedNetXirr => _summary?.ConvertedNetXirr;

    public string ReportingCurrency => _summary?.ReportingCurrency ?? string.Empty;

    public bool IsReportingCurrencyEnabled => _summary?.IsReportingCurrencyEnabled ?? false;

    public bool IsReportingCurrencyUnavailable => IsReportingCurrencyEnabled && _summary!.IsReportingCurrencyUnavailable;

    public bool ShowConvertedTotals => IsReportingCurrencyEnabled && !IsReportingCurrencyUnavailable;

    public bool IsReportingCurrencyPartial => ShowConvertedTotals && _summary!.IsReportingCurrencyPartial;

    public bool IsPartial => _summary?.IsPartial ?? false;

    public int UnvaluedHoldingCount => _summary?.UnvaluedHoldingCount ?? 0;

    public string? PartialNoticeText => IsPartial && UnvaluedHoldingCount > 0
        ? $"{UnvaluedHoldingCount} {(UnvaluedHoldingCount == 1 ? "holding" : "holdings")} could not be valued; Market Value, Unrealised Gain/Loss and both XIRR figures are incomplete."
        : null;

    public bool ShowPartialNotice => PartialNoticeText != null;

    public Task LoadAsync() => ExecuteRefreshAsync(
        () => ++_requestId,
        id => id == _requestId,
        loading => IsLoading = loading,
        error => ErrorMessage = error,
        async isCurrent =>
        {
            var summary = await _dashboardService.GetDashboardAsync();

            if (!isCurrent())
            {
                return;
            }

            _summary = summary;
            NotifySummaryChanged();
        },
        ex => _logger.LogError("Portfolio dashboard refresh failed with {ErrorType}", ex.GetType().Name));

    private void NotifySummaryChanged()
    {
        foreach (var property in SummaryProperties)
        {
            OnPropertyChanged(property);
        }
    }
}
