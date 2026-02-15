using System.Collections.ObjectModel;
using Financial.Application.DTO;

namespace FinancialUI.ViewModels;

/// <summary>
/// ViewModel for displaying asset details with operations and credits in tabs
/// </summary>
public class AssetDetailsViewModel : ViewModelBase
{
    private string _assetName = string.Empty;
    private string _brokerName = string.Empty;
    private string _portfolioName = string.Empty;
    private string _ticker = string.Empty;
    private string _isin = string.Empty;
    private string _exchange = string.Empty;
    private decimal _quantity;
    private decimal _averagePrice;
    private bool _isActive;
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalCredits;

    public string AssetName
    {
        get => _assetName;
        private set => SetProperty(ref _assetName, value);
    }

    public string BrokerName
    {
        get => _brokerName;
        private set => SetProperty(ref _brokerName, value);
    }

    public string PortfolioName
    {
        get => _portfolioName;
        private set => SetProperty(ref _portfolioName, value);
    }

    public string Ticker
    {
        get => _ticker;
        private set => SetProperty(ref _ticker, value);
    }

    public string ISIN
    {
        get => _isin;
        private set => SetProperty(ref _isin, value);
    }

    public string Exchange
    {
        get => _exchange;
        private set => SetProperty(ref _exchange, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        private set => SetProperty(ref _quantity, value);
    }

    public decimal AveragePrice
    {
        get => _averagePrice;
        private set => SetProperty(ref _averagePrice, value);
    }

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public decimal TotalBought
    {
        get => _totalBought;
        private set => SetProperty(ref _totalBought, value);
    }

    public decimal TotalSold
    {
        get => _totalSold;
        private set => SetProperty(ref _totalSold, value);
    }

    public decimal TotalCredits
    {
        get => _totalCredits;
        private set => SetProperty(ref _totalCredits, value);
    }

    public ObservableCollection<OperationDTO> Operations { get; } = new();
    public ObservableCollection<CreditDTO> Credits { get; } = new();

    public AssetDetailsViewModel()
    {
        // Default constructor for design-time support
    }

    /// <summary>
    /// Loads asset details from DTO
    /// </summary>
    public void LoadAssetDetails(AssetDetailsDTO details)
    {
        AssetName = details.Name;
        BrokerName = details.BrokerName;
        PortfolioName = details.PortfolioName;
        Ticker = details.Ticker;
        ISIN = details.ISIN;
        Exchange = details.Exchange;
        Quantity = details.Quantity;
        AveragePrice = details.AveragePrice;
        IsActive = details.IsActive;
        TotalBought = details.TotalBought;
        TotalSold = details.TotalSold;
        TotalCredits = details.TotalCredits;

        Operations.Clear();
        foreach (var op in details.Operations)
        {
            Operations.Add(op);
        }

        Credits.Clear();
        foreach (var credit in details.Credits)
        {
            Credits.Add(credit);
        }
    }

    /// <summary>
    /// Clears all asset details
    /// </summary>
    public void Clear()
    {
        AssetName = string.Empty;
        BrokerName = string.Empty;
        PortfolioName = string.Empty;
        Ticker = string.Empty;
        ISIN = string.Empty;
        Exchange = string.Empty;
        Quantity = 0;
        AveragePrice = 0;
        IsActive = false;
        TotalBought = 0;
        TotalSold = 0;
        TotalCredits = 0;
        Operations.Clear();
        Credits.Clear();
    }
}
