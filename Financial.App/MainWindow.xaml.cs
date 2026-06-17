using Financial.Application.DTOs;
using Financial.Domain.Rules;
using Financial.Application.Interfaces;
using Financial.Presentation.App.Components;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.Options;
using Financial.Presentation.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App
{
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly IAssetPriceService _assetPriceService;
        private readonly IDividendService _dividendService;
        private readonly MainNavigationViewModel _navigationViewModel;

        private const string BvmfExchange = "BVMF";
        private const string XpiBrokerName = "XPI";
        private const string DefaultPortfolioName = "Default";
        private const string AcoesPortfolioName = "Acoes";
        private const int ProgressHideDelayMs = 2000;

        public MainNavigationViewModel NavigationViewModel => _navigationViewModel;

        public MainWindow(
            INavigationService navigationService,
            IAssetPriceService assetPriceService,
            IDividendService dividendService,
            MainNavigationViewModel navigationViewModel)
        {
            _navigationService = navigationService;
            _assetPriceService = assetPriceService;
            _dividendService = dividendService;
            _navigationViewModel = navigationViewModel;

            InitializeComponent();

            var groupedOptions = new ListCollectionView(new List<WatchlistItem>(WatchlistOptions.DefaultDividendWatchlist));
            groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            txtTicker.ItemsSource = groupedOptions;

            Loaded += async (s, e) =>
            {
                var navView = FindNavigationView(this);
                if (navView != null)
                {
                    navView.DataContext = _navigationViewModel;
                }
                await _navigationViewModel.LoadNavigationTreeAsync();
            };
        }

        private NavigationView? FindNavigationView(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is NavigationView navView)
                    return navView;

                var result = FindNavigationView(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        public class AssetValue
        {
            public required string Exchange { get; set; }
            public required string Ticker { get; set; }
            public required string Name { get; set; }
            public decimal Price { get; set; }
        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            var ticker = txtTicker.Text.ToUpperInvariant();
            var request = new DividendLookupRequestDTO { Exchange = BvmfExchange, Ticker = ticker };

            var summary = _dividendService.GetDividendSummary(request);

            dividendDataGrid.ItemsSource = summary.History;
            dividendByYearDataGrid.ItemsSource = summary.YearTotals;

            lblName.Text = $"{summary.Ticker} - {summary.Name}";
            lblPrice.Text = $"Current price: {summary.CurrentPrice:N2}";
            lblAverageDividend.Text = $"Average Dividend: {summary.AverageDividendPerYear:F2} (last {DividendValuationRules.DividendYearsLookback} years)";
            lblPriceMax.Text = $"Price max buy: {summary.PriceMaxBuy:F2}   Discount {summary.DiscountPercent:F2}%";
            lblPriceMax.Foreground = summary.PriceMaxBuy > summary.CurrentPrice ? Brushes.Green : Brushes.Red;
        }

        private void DividendDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
            {
                var dataGridColumn = e.Column as DataGridTextColumn;
                if (dataGridColumn != null)
                {
                    dataGridColumn.Binding.StringFormat = DateFormatHelper.GetPaddedShortDatePattern();
                }
            }

            ApplyValueColumnStyle(e, "Value");
        }

        private void DividendByYearDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            ApplyValueColumnStyle(e, "Total");
        }

        private static void ApplyValueColumnStyle(DataGridAutoGeneratingColumnEventArgs e, string propertyName)
        {
            if (!string.Equals(e.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (e.Column is not DataGridTextColumn dataGridColumn)
            {
                return;
            }

            if (dataGridColumn.Binding is Binding binding)
            {
                binding.StringFormat = "N2";
            }
            else
            {
                dataGridColumn.Binding = new Binding(propertyName) { StringFormat = "N2" };
            }

            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            dataGridColumn.ElementStyle = style;
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
}
