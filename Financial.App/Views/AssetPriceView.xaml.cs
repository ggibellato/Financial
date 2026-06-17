using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Views;

public partial class AssetPriceView : UserControl
{
    private readonly INavigationService _navigationService;
    private readonly IAssetPriceService _assetPriceService;

    private const string XpiBrokerName = "XPI";
    private const string DefaultPortfolioName = "Default";
    private const string AcoesPortfolioName = "Acoes";
    private const int ProgressHideDelayMs = 2000;

    public class AssetValue
    {
        public required string Exchange { get; set; }
        public required string Ticker { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
    }

    public AssetPriceView(INavigationService navigationService, IAssetPriceService assetPriceService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));

        InitializeComponent();
    }

    private async void btnCheckFIIS_Click(object sender, RoutedEventArgs e)
    {
        btnCheckFIIS.IsEnabled = false;
        pnlFIIsProgress.Visibility = Visibility.Visible;
        fiisPriceDataGrid.ItemsSource = null;

        try
        {
            var assets = _navigationService.GetAssetsByBrokerPortfolio(XpiBrokerName, DefaultPortfolioName).ToList();
            var acoes = _navigationService.GetAssetsByBrokerPortfolio(XpiBrokerName, AcoesPortfolioName);
            assets.AddRange(acoes);

            var list = new List<AssetValue>();
            var totalAssets = assets.Count;
            var currentAsset = 0;

            foreach (var asset in assets)
            {
                currentAsset++;

                var progressPercent = (currentAsset * 100.0) / totalAssets;
                pgFIIsProgress.Value = progressPercent;
                lblFIIsProgress.Text = $"Fetching {currentAsset} of {totalAssets}: {asset.Ticker}...";

                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                var value = await Task.Run(() => _assetPriceService.GetCurrentPrice(new AssetPriceRequestDTO
                {
                    Exchange = asset.Exchange,
                    Ticker = asset.Ticker,
                }));
                list.Add(new AssetValue { Exchange = value.Exchange, Ticker = value.Ticker, Name = value.Name, Price = value.Price });

                fiisPriceDataGrid.ItemsSource = null;
                fiisPriceDataGrid.ItemsSource = list;
            }

            lblFIIsProgress.Text = $"Completed! Loaded {totalAssets} assets.";
        }
        catch (Exception ex)
        {
            lblFIIsProgress.Text = $"Error: {ex.Message}";
            MessageBox.Show($"An error occurred while fetching prices:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            await Task.Delay(ProgressHideDelayMs);
            btnCheckFIIS.IsEnabled = true;
            pnlFIIsProgress.Visibility = Visibility.Collapsed;
            pgFIIsProgress.Value = 0;
        }
    }
}
