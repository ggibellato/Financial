using Financial.Application.DTOs;

namespace Financial.Presentation.App.ViewModels;

public class PortfolioAssetSummaryRowViewModel : ViewModelBase
{
    private bool _isLoadingPrice = true;
    private bool _priceFetchFailed;
    private decimal? _currentValue;
    private decimal? _profitPercent;

    public string AssetName { get; }
    public string Ticker { get; }
    public string Exchange { get; }
    public DateTime? FirstInvestmentDate { get; }
    public decimal CurrentQuantity { get; }
    public decimal TotalInvested { get; }
    public decimal PortfolioWeight { get; }

    public bool IsLoadingPrice => _isLoadingPrice;
    public bool PriceFetchFailed => _priceFetchFailed;
    public decimal? CurrentValue => _currentValue;
    public decimal? ProfitPercent => _profitPercent;

    public string DisplayFirstInvestmentDate =>
        FirstInvestmentDate.HasValue ? FirstInvestmentDate.Value.ToString("dd/MM/yyyy") : string.Empty;

    public string DisplayCurrentQuantity => CurrentQuantity.ToString("N8");
    public string DisplayTotalInvested => TotalInvested.ToString("N2");
    public string DisplayPortfolioWeight => $"{PortfolioWeight:F1}%";

    public string DisplayCurrentValue =>
        _isLoadingPrice || _priceFetchFailed || !_currentValue.HasValue ? "—" : _currentValue.Value.ToString("N2");

    public string DisplayProfitPercent =>
        _isLoadingPrice || _priceFetchFailed || !_profitPercent.HasValue ? "—" : $"{_profitPercent.Value:F2}%";

    public bool ProfitIsPositive => _currentValue.HasValue && _currentValue.Value > TotalInvested;
    public bool ProfitIsNegative => _currentValue.HasValue && _currentValue.Value < TotalInvested;

    public PortfolioAssetSummaryRowViewModel(PortfolioAssetSummaryItemDTO dto)
    {
        AssetName = dto.AssetName;
        Ticker = dto.Ticker;
        Exchange = dto.Exchange;
        FirstInvestmentDate = dto.FirstInvestmentDate;
        CurrentQuantity = dto.CurrentQuantity;
        TotalInvested = dto.TotalInvested;
        PortfolioWeight = dto.PortfolioWeight;
    }

    public void ApplyPrice(decimal price)
    {
        _currentValue = price * CurrentQuantity;
        _profitPercent = TotalInvested != 0
            ? (_currentValue.Value - TotalInvested) / TotalInvested * 100
            : (decimal?)null;
        _isLoadingPrice = false;
        OnPropertyChanged(nameof(IsLoadingPrice));
        OnPropertyChanged(nameof(DisplayCurrentValue));
        OnPropertyChanged(nameof(DisplayProfitPercent));
        OnPropertyChanged(nameof(ProfitIsPositive));
        OnPropertyChanged(nameof(ProfitIsNegative));
    }

    public void MarkPriceFailed()
    {
        _isLoadingPrice = false;
        _priceFetchFailed = true;
        OnPropertyChanged(nameof(IsLoadingPrice));
        OnPropertyChanged(nameof(PriceFetchFailed));
        OnPropertyChanged(nameof(DisplayCurrentValue));
        OnPropertyChanged(nameof(DisplayProfitPercent));
    }
}
