using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

public class AssetPriceFetchViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAssetPriceLookupService _priceService;
    private readonly IReadOnlyList<PortfolioReferenceDTO> _portfolios;
    private readonly Action<string> _showError;
    private bool _isFetching;
    private string _progressMessage = string.Empty;
    private double _progressPercent;

    private const int ProgressHideDelayMs = 2000;

    public bool IsFetching
    {
        get => _isFetching;
        private set => SetProperty(ref _isFetching, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        private set => SetProperty(ref _progressMessage, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => SetProperty(ref _progressPercent, value);
    }

    public ObservableCollection<AssetPriceFetchResult> Results { get; } = new();
    public RelayCommand FetchCommand { get; }

    public AssetPriceFetchViewModel(
        INavigationService navigationService,
        IAssetPriceLookupService priceService,
        IOptions<AssetPriceFetchOptions> options,
        Action<string> showError)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _showError = showError ?? throw new ArgumentNullException(nameof(showError));
        _portfolios = (options?.Value.Portfolios ?? new List<PortfolioReferenceDTO>()).AsReadOnly();
        FetchCommand = new RelayCommand(async () => await FetchAsync(), () => !IsFetching);
    }

    private async Task FetchAsync()
    {
        IsFetching = true;
        FetchCommand.RaiseCanExecuteChanged();
        Results.Clear();

        try
        {
            var assets = _portfolios
                .SelectMany(p => _navigationService.GetAssetsByBrokerPortfolio(p.BrokerName, p.PortfolioName)
                    .Select(asset => (BrokerName: p.BrokerName, PortfolioName: p.PortfolioName, Asset: asset)))
                .ToList();

            var total = assets.Count;
            for (var i = 0; i < assets.Count; i++)
            {
                var (brokerName, portfolioName, asset) = assets[i];
                ProgressPercent = (i + 1) * 100.0 / total;
                ProgressMessage = $"Fetching {i + 1} of {total}: {asset.Ticker}...";

                // Per-asset, so one failing fetch does not end the run for every asset after it -
                // the batch is what builds Price History, so an aborted run leaves silent gaps
                // rather than a screenful of "3 failed".
                try
                {
                    // Supplying the broker, portfolio and asset name is what makes the orchestration
                    // record the fetched price into Price History, the same as the per-asset Refresh
                    // button and the portfolio grid. Without them this screen took the lookup-only
                    // path and built no history at all.
                    var price = await _priceService.GetCurrentPriceAsync(new AssetPriceRequestDTO
                    {
                        Exchange = asset.Exchange,
                        Ticker = asset.Ticker,
                        AssetClass = asset.Class,
                        BrokerName = brokerName,
                        Name = asset.Name,
                        PortfolioName = portfolioName,
                        AssetName = asset.Name
                    });
                    Results.Add(AssetPriceFetchResult.Success(price.Ticker, price.Name, price.Price));
                }
                catch (Exception ex)
                {
                    Results.Add(AssetPriceFetchResult.Failure(asset.Ticker, asset.Name, ex.Message));
                }
            }

            ProgressMessage = $"Completed! Loaded {total} assets.";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Error: {ex.Message}";
            _showError($"An error occurred while fetching prices:\n{ex.Message}");
        }
        finally
        {
            await Task.Delay(ProgressHideDelayMs);
            IsFetching = false;
            ProgressPercent = 0;
            FetchCommand.RaiseCanExecuteChanged();
        }
    }
}
