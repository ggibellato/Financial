using System.Collections.ObjectModel;
using Financial.Application.Configuration;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Rules;
using Microsoft.Extensions.Options;

namespace Financial.Presentation.App.ViewModels;

public class DividendCheckViewModel : ViewModelBase
{
    private readonly IDividendService _dividendService;
    private readonly string _defaultExchange;
    private string _ticker = string.Empty;
    private string _summaryName = string.Empty;
    private string _summaryPrice = string.Empty;
    private string _summaryAverageDividend = string.Empty;
    private string _summaryPriceMaxBuy = string.Empty;
    private bool _isPriceGood;

    public string Ticker
    {
        get => _ticker;
        set => SetProperty(ref _ticker, value);
    }

    public string SummaryName
    {
        get => _summaryName;
        private set => SetProperty(ref _summaryName, value);
    }

    public string SummaryPrice
    {
        get => _summaryPrice;
        private set => SetProperty(ref _summaryPrice, value);
    }

    public string SummaryAverageDividend
    {
        get => _summaryAverageDividend;
        private set => SetProperty(ref _summaryAverageDividend, value);
    }

    public string SummaryPriceMaxBuy
    {
        get => _summaryPriceMaxBuy;
        private set => SetProperty(ref _summaryPriceMaxBuy, value);
    }

    public bool IsPriceGood
    {
        get => _isPriceGood;
        private set => SetProperty(ref _isPriceGood, value);
    }

    public ObservableCollection<DividendHistoryItemDTO> History { get; } = new();
    public ObservableCollection<DividendYearTotalDTO> YearTotals { get; } = new();
    public RelayCommand CheckCommand { get; }

    public DividendCheckViewModel(IDividendService dividendService, IOptions<DividendOptions> dividendOptions)
    {
        _dividendService = dividendService ?? throw new ArgumentNullException(nameof(dividendService));
        _defaultExchange = dividendOptions?.Value.DefaultExchange
            ?? throw new ArgumentNullException(nameof(dividendOptions));
        CheckCommand = new RelayCommand(Check);
    }

    private void Check()
    {
        var request = new DividendLookupRequestDTO { Exchange = _defaultExchange, Ticker = Ticker.ToUpperInvariant() };
        var summary = _dividendService.GetDividendSummary(request);

        History.Clear();
        foreach (var item in summary.History)
            History.Add(item);

        YearTotals.Clear();
        foreach (var item in summary.YearTotals)
            YearTotals.Add(item);

        SummaryName = $"{summary.Ticker} - {summary.Name}";
        SummaryPrice = $"Current price: {summary.CurrentPrice:N2}";
        SummaryAverageDividend = $"Average Dividend: {summary.AverageDividendPerYear:F2} (last {DividendValuationRules.DividendYearsLookback} years)";
        SummaryPriceMaxBuy = $"Price max buy: {summary.PriceMaxBuy:F2}   Discount {summary.DiscountPercent:F2}%";
        IsPriceGood = summary.PriceMaxBuy > summary.CurrentPrice;
    }
}
