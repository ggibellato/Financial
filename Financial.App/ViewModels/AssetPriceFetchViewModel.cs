using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.Options;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels;

public class AssetPriceFetchViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAssetPriceService _assetPriceService;
    private readonly IReadOnlyList<PortfolioReference> _portfolios;
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

    public ObservableCollection<AssetPriceDTO> Results { get; } = new();
    public RelayCommand FetchCommand { get; }

    public AssetPriceFetchViewModel(
        INavigationService navigationService,
        IAssetPriceService assetPriceService,
        IOptions<AssetPriceFetchOptions> options,
        Action<string> showError)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _showError = showError ?? throw new ArgumentNullException(nameof(showError));
        _portfolios = (options?.Value.Portfolios ?? new List<PortfolioReference>()).AsReadOnly();
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
                .SelectMany(p => _navigationService.GetAssetsByBrokerPortfolio(p.BrokerName, p.PortfolioName))
                .ToList();

            var total = assets.Count;
            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                ProgressPercent = (i + 1) * 100.0 / total;
                ProgressMessage = $"Fetching {i + 1} of {total}: {asset.Ticker}...";

                var result = await Task.Run(() => _assetPriceService.GetCurrentPrice(new AssetPriceRequestDTO
                {
                    Exchange = asset.Exchange,
                    Ticker = asset.Ticker
                }));
                Results.Add(result);
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
