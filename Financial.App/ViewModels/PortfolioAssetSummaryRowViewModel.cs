using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using System.Globalization;

namespace Financial.Presentation.App.ViewModels;

public class PortfolioAssetSummaryRowViewModel : ViewModelBase
{
    private readonly IXirrCalculationService _xirrCalculationService;
    private readonly IProfitCalculationService _profitCalculationService;

    private bool _isLoadingPrice = true;
    private bool _priceFetchFailed;
    private decimal? _currentPrice;
    private decimal? _currentValue;
    private decimal? _profitPercent;
    private decimal? _profitWithCreditsPercent;
    private decimal? _xirr;

    public string AssetName { get; }
    public string Ticker { get; }
    public string Exchange { get; }
    public GlobalAssetClass Class { get; }
    public DateTime? FirstInvestmentDate { get; }
    public decimal CurrentQuantity { get; }
    public decimal AveragePrice { get; }
    public decimal? AverageSellPrice { get; }
    public decimal TotalBought { get; }
    public decimal TotalInvested { get; }
    public decimal RealizedGainLoss { get; }
    public decimal PortfolioWeight { get; }
    public decimal TotalCredits { get; }
    public IReadOnlyList<AssetCashFlowDTO> CashFlows { get; }
    public decimal LastMonthCredits { get; }
    public string? LastCreditMonth { get; }
    public decimal? LastMonthCreditsPercent { get; }
    public decimal? EstimatedAnnualCredits { get; }
    public decimal? EstimatedAnnualPercent { get; }
    public decimal CurrentMonthCredits { get; }

    public bool IsLoadingPrice => _isLoadingPrice;
    public bool PriceFetchFailed => _priceFetchFailed;
    public decimal? CurrentPrice => _currentPrice;
    public decimal? CurrentValue => _currentValue;
    public decimal? ProfitPercent => _profitPercent;
    public decimal? ProfitWithCreditsPercent => _profitWithCreditsPercent;
    public decimal? Xirr => _xirr;

    public string DisplayFirstInvestmentDate =>
        FirstInvestmentDate.HasValue ? FirstInvestmentDate.Value.ToString("dd/MM/yyyy") : string.Empty;

    public string DisplayCurrentQuantity => CurrentQuantity.ToString("N8");
    public string DisplayAveragePrice => AveragePrice.ToString("N2");
    public string DisplayTotalInvested => TotalInvested.ToString("N2");
    public string DisplayPortfolioWeight => $"{PortfolioWeight:F1}%";
    public string DisplayTotalCredits => TotalCredits.ToString("N2");

    public string DisplayLastMonthCredits =>
        LastCreditMonth is null ? "—" : LastMonthCredits.ToString("N2");

    public string LastCreditMonthDisplay =>
        LastCreditMonth is null
            ? "—"
            : DateTime.ParseExact(LastCreditMonth, "yyyy-MM", CultureInfo.InvariantCulture)
                      .ToString("MMM yyyy", CultureInfo.InvariantCulture);

    public string DisplayLastMonthCreditsPercent =>
        LastMonthCreditsPercent.HasValue ? $"{LastMonthCreditsPercent.Value:F2}%" : "—";

    public string DisplayEstimatedAnnualCredits =>
        EstimatedAnnualCredits.HasValue ? EstimatedAnnualCredits.Value.ToString("N2") : "—";

    public string DisplayEstimatedAnnualPercent =>
        EstimatedAnnualPercent.HasValue ? $"{EstimatedAnnualPercent.Value:F2}%" : "—";

    public string DisplayCurrentValue =>
        _isLoadingPrice || _priceFetchFailed ? "—" : _currentValue?.ToString("N2") ?? "—";

    public string DisplayCurrentPrice =>
        _isLoadingPrice || _priceFetchFailed ? "—" : _currentPrice?.ToString("N2") ?? "—";

    public string DisplayProfitPercent =>
        _isLoadingPrice || _priceFetchFailed || !_profitPercent.HasValue ? "—" : $"{_profitPercent.Value:F2}%";

    public string DisplayProfitWithCreditsPercent =>
        _isLoadingPrice || _priceFetchFailed || !_profitWithCreditsPercent.HasValue ? "—" : $"{_profitWithCreditsPercent.Value:F2}%";

    public string DisplayXirr =>
        _isLoadingPrice || _priceFetchFailed || !_xirr.HasValue ? "—" : $"{_xirr.Value:F2}%";

    public bool ProfitIsPositive => _profitPercent > 0;
    public bool ProfitIsNegative => _profitPercent < 0;
    public bool ProfitWithCreditsIsPositive => _profitWithCreditsPercent > 0;
    public bool ProfitWithCreditsIsNegative => _profitWithCreditsPercent < 0;
    public bool XirrIsPositive => _xirr > 0;
    public bool XirrIsNegative => _xirr < 0;

    // Historic (closed) positions have no live price to mark-to-market: these are derived
    // entirely from already-realized cash flows (bought/sold/credits), available synchronously
    // — unlike the Active-scope fields above, which depend on an async price fetch.
    public string DisplayRealizedGainLoss => RealizedGainLoss.ToString("N2");
    public bool RealizedGainLossIsPositive => RealizedGainLoss > 0;
    public bool RealizedGainLossIsNegative => RealizedGainLoss < 0;

    public string DisplaySoldPrice => AverageSellPrice.HasValue ? AverageSellPrice.Value.ToString("N2") : "—";

    // Realized capital gain alone (credits excluded), matching the active-scope semantic
    // where credits are a separate "w/ Credits" column.
    public decimal? HistoricProfitPercent =>
        TotalBought != 0 ? (RealizedGainLoss - TotalCredits) / TotalBought * 100 : null;

    public decimal? HistoricProfitWithCreditsPercent =>
        TotalBought != 0 ? RealizedGainLoss / TotalBought * 100 : null;

    // Historic cash flows already contain every buy/sell/credit as a dated entry, so the
    // terminal value is 0 (no remaining position left to mark-to-market).
    public decimal? HistoricXirr
    {
        get
        {
            var fraction = _xirrCalculationService.Calculate(CashFlows, 0m);
            return fraction.HasValue ? fraction.Value * 100 : null;
        }
    }

    public string DisplayHistoricProfitPercent =>
        HistoricProfitPercent.HasValue ? $"{HistoricProfitPercent.Value:F2}%" : "—";

    public string DisplayHistoricProfitWithCreditsPercent =>
        HistoricProfitWithCreditsPercent.HasValue ? $"{HistoricProfitWithCreditsPercent.Value:F2}%" : "—";

    public string DisplayHistoricXirr =>
        HistoricXirr.HasValue ? $"{HistoricXirr.Value:F2}%" : "—";

    public bool HistoricProfitIsPositive => HistoricProfitPercent > 0;
    public bool HistoricProfitIsNegative => HistoricProfitPercent < 0;
    public bool HistoricProfitWithCreditsIsPositive => HistoricProfitWithCreditsPercent > 0;
    public bool HistoricProfitWithCreditsIsNegative => HistoricProfitWithCreditsPercent < 0;
    public bool HistoricXirrIsPositive => HistoricXirr > 0;
    public bool HistoricXirrIsNegative => HistoricXirr < 0;

    public PortfolioAssetSummaryRowViewModel(PortfolioAssetSummaryItemDTO dto, IXirrCalculationService xirrCalculationService, IProfitCalculationService profitCalculationService)
    {
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
        _profitCalculationService = profitCalculationService ?? throw new ArgumentNullException(nameof(profitCalculationService));
        AssetName = dto.AssetName;
        Ticker = dto.Ticker;
        Exchange = dto.Exchange;
        Class = dto.Class;
        FirstInvestmentDate = dto.FirstInvestmentDate;
        CurrentQuantity = dto.CurrentQuantity;
        AveragePrice = dto.AveragePrice;
        AverageSellPrice = dto.AverageSellPrice;
        TotalBought = dto.TotalBought;
        TotalInvested = dto.TotalInvested;
        RealizedGainLoss = dto.RealizedGainLoss;
        PortfolioWeight = dto.PortfolioWeight;
        TotalCredits = dto.TotalCredits;
        CashFlows = dto.CashFlows;
        LastMonthCredits = dto.LastMonthCredits;
        LastCreditMonth = dto.LastCreditMonth;
        LastMonthCreditsPercent = dto.LastMonthCreditsPercent;
        EstimatedAnnualCredits = dto.EstimatedAnnualCredits;
        EstimatedAnnualPercent = dto.EstimatedAnnualPercent;
        CurrentMonthCredits = dto.CurrentMonthCredits;
    }

    public void ApplyPrice(decimal price)
    {
        _currentPrice = price;
        _currentValue = price * CurrentQuantity;

        var costBasis = CurrentQuantity * AveragePrice;

        _profitPercent = _profitCalculationService.CalculateProfitPercent(_currentValue.Value, costBasis);
        _profitWithCreditsPercent = _profitCalculationService.CalculateProfitPercent(_currentValue.Value + TotalCredits, costBasis);

        var xirrFraction = _xirrCalculationService.Calculate(CashFlows, _currentValue.Value);
        _xirr = xirrFraction.HasValue ? xirrFraction.Value * 100 : null;
        _isLoadingPrice = false;

        OnPropertyChanged(nameof(IsLoadingPrice));
        OnPropertyChanged(nameof(CurrentPrice));
        OnPropertyChanged(nameof(DisplayCurrentPrice));
        OnPropertyChanged(nameof(CurrentValue));
        OnPropertyChanged(nameof(DisplayCurrentValue));
        OnPropertyChanged(nameof(ProfitPercent));
        OnPropertyChanged(nameof(DisplayProfitPercent));
        OnPropertyChanged(nameof(ProfitIsPositive));
        OnPropertyChanged(nameof(ProfitIsNegative));
        OnPropertyChanged(nameof(ProfitWithCreditsPercent));
        OnPropertyChanged(nameof(DisplayProfitWithCreditsPercent));
        OnPropertyChanged(nameof(ProfitWithCreditsIsPositive));
        OnPropertyChanged(nameof(ProfitWithCreditsIsNegative));
        OnPropertyChanged(nameof(Xirr));
        OnPropertyChanged(nameof(DisplayXirr));
        OnPropertyChanged(nameof(XirrIsPositive));
        OnPropertyChanged(nameof(XirrIsNegative));
    }

    public void MarkPriceFailed()
    {
        _priceFetchFailed = true;
        RaisePriceUnavailableNotifications();
        OnPropertyChanged(nameof(PriceFetchFailed));
    }

    public void MarkPriceNotApplicable()
    {
        RaisePriceUnavailableNotifications();
    }

    private void RaisePriceUnavailableNotifications()
    {
        _isLoadingPrice = false;

        OnPropertyChanged(nameof(IsLoadingPrice));
        OnPropertyChanged(nameof(DisplayCurrentPrice));
        OnPropertyChanged(nameof(DisplayCurrentValue));
        OnPropertyChanged(nameof(DisplayProfitPercent));
        OnPropertyChanged(nameof(DisplayProfitWithCreditsPercent));
        OnPropertyChanged(nameof(DisplayXirr));
    }

}
