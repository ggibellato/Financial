namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Shared pixel widths for the Portfolio Summary grid's columns, bound TwoWay from the
/// DataGrid's own columns (so user resizing updates them) and OneWay into the group-header
/// overlay bar's ColumnDefinitions (so the overlay stays aligned with the resized columns).
/// </summary>
public class PortfolioSummaryColumnWidths : ViewModelBase
{
    private double _assetName = 160;
    private double _firstInvestment = 110;
    private double _quantity = 110;
    private double _totalInvested = 110;
    private double _portfolioWeight = 90;
    private double _totalCredits = 100;
    private double _currentValue = 100;
    private double _averagePrice = 100;
    private double _profit = 70;
    private double _profitWithCredits = 110;
    private double _xirr = 70;
    private double _lastMonthCredits = 100;
    private double _lastCreditMonth = 90;
    private double _lastMonthPercent = 70;
    private double _estAnnualCredits = 100;
    private double _estAnnualPercent = 70;

    public double AssetName { get => _assetName; set => SetProperty(ref _assetName, value); }
    public double FirstInvestment { get => _firstInvestment; set => SetProperty(ref _firstInvestment, value); }
    public double Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
    public double TotalInvested { get => _totalInvested; set => SetProperty(ref _totalInvested, value); }
    public double PortfolioWeight { get => _portfolioWeight; set => SetProperty(ref _portfolioWeight, value); }
    public double TotalCredits { get => _totalCredits; set => SetProperty(ref _totalCredits, value); }
    public double CurrentValue { get => _currentValue; set => SetProperty(ref _currentValue, value); }
    public double AveragePrice { get => _averagePrice; set => SetProperty(ref _averagePrice, value); }
    public double Profit { get => _profit; set => SetProperty(ref _profit, value); }
    public double ProfitWithCredits { get => _profitWithCredits; set => SetProperty(ref _profitWithCredits, value); }
    public double Xirr { get => _xirr; set => SetProperty(ref _xirr, value); }
    public double LastMonthCredits { get => _lastMonthCredits; set => SetProperty(ref _lastMonthCredits, value); }
    public double LastCreditMonth { get => _lastCreditMonth; set => SetProperty(ref _lastCreditMonth, value); }
    public double LastMonthPercent { get => _lastMonthPercent; set => SetProperty(ref _lastMonthPercent, value); }
    public double EstAnnualCredits { get => _estAnnualCredits; set => SetProperty(ref _estAnnualCredits, value); }
    public double EstAnnualPercent { get => _estAnnualPercent; set => SetProperty(ref _estAnnualPercent, value); }
}
