using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.Components;
using Financial.Presentation.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App
{
    public partial class MainWindow : Window
    {
        private readonly IRepository _repository;
        private readonly IAssetPriceService _assetPriceService;
        private readonly IDividendService _dividendService;
        private readonly MainNavigationViewModel _navigationViewModel;

        private const string BvmfExchange = "BVMF";

        public MainNavigationViewModel NavigationViewModel => _navigationViewModel;

        public MainWindow(
            IRepository repository,
            IAssetPriceService assetPriceService,
            IDividendService dividendService,
            MainNavigationViewModel navigationViewModel)
        {
            _repository = repository;
            _assetPriceService = assetPriceService;
            _dividendService = dividendService;
            _navigationViewModel = navigationViewModel;

            InitializeComponent();

            List<Option> options = new List<Option>
            {
                new Option { Group = "Ja possuidas", Name = "KLBN4" },
                new Option { Group = "Ja possuidas", Name = "TASA4" },
                new Option { Group = "Ja possuidas", Name = "TAEE3" },
                new Option { Group = "Outras Barse", Name = "UNIP6" },
                new Option { Group = "Outras Barse", Name = "CMIG4" },
                new Option { Group = "Outras Barse", Name = "TRPL4" },
                new Option { Group = "Outras Barse", Name = "BBAS3" },
                new Option { Group = "Outras", Name = "CSAN3" },
            };

            var groupedOptions = new ListCollectionView(options);
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

        public class Option
        {
            public required string Group { get; set; }
            public required string Name { get; set; }
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

            var value = _assetPriceService.GetCurrentPrice(new AssetPriceRequestDTO
            {
                Exchange = BvmfExchange,
                Ticker = ticker,
            });

            var dividends = _dividendService.GetDividendHistory(new DividendLookupRequestDTO
            {
                Exchange = BvmfExchange,
                Ticker = ticker,
            });

            dividendDataGrid.ItemsSource = dividends;

            var dividendsByYear = dividends
                .GroupBy(d => d.Date.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Total = g.Sum(gi => gi.Value)
                })
                .OrderByDescending(dy => dy.Year);
            dividendByYearDataGrid.ItemsSource = dividendsByYear;

            var averageDividend = dividendsByYear.Where(dy => dy.Year < DateTime.Now.Year).Take(5).Average(dy => dy.Total);
            var priceMax = averageDividend / (decimal)0.06;
            var priceDiscountPer = (1 - value.Price / priceMax) * 100;

            lblName.Text = $"{value.Ticker} - {value.Name}";
            lblPrice.Text = $"Current price: {value.Price:N2}";
            lblAverageDividend.Text = $"Average Dividend: {averageDividend:F2} (last 5 years)";
            lblPriceMax.Text = $"Price max buy: {priceMax:F2}   Discount {priceDiscountPer:F2}%";
            lblPriceMax.Foreground = priceMax > value.Price ? Brushes.Green : Brushes.Red;
        }

        private void DividendDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
            {
                var dataGridColumn = e.Column as DataGridTextColumn;
                if (dataGridColumn != null)
                {
                    dataGridColumn.Binding.StringFormat = GetPaddedShortDatePattern();
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

        private static string GetPaddedShortDatePattern()
        {
            var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            return PadDayMonthTokens(pattern);
        }

        private static string PadDayMonthTokens(string pattern)
        {
            var sb = new StringBuilder();
            bool inQuote = false;

            for (int i = 0; i < pattern.Length; i++)
            {
                var ch = pattern[i];
                if (ch == '\'')
                {
                    sb.Append(ch);
                    if (i + 1 < pattern.Length && pattern[i + 1] == '\'')
                    {
                        sb.Append(pattern[i + 1]);
                        i++;
                    }
                    else
                    {
                        inQuote = !inQuote;
                    }
                    continue;
                }

                if (inQuote)
                {
                    sb.Append(ch);
                    continue;
                }

                if (ch == 'd' || ch == 'M')
                {
                    int count = 1;
                    while (i + count < pattern.Length && pattern[i + count] == ch)
                    {
                        count++;
                    }

                    if (count == 1)
                    {
                        sb.Append(ch, 2);
                    }
                    else
                    {
                        sb.Append(ch, count);
                    }

                    i += count - 1;
                    continue;
                }

                sb.Append(ch);
            }

            return sb.ToString();
        }

        private async void btnCheckFIIS_Click(object sender, RoutedEventArgs e)
        {
            btnCheckFIIS.IsEnabled = false;
            pnlFIIsProgress.Visibility = Visibility.Visible;
            fiisPriceDataGrid.ItemsSource = null;

            try
            {
                var assets = _repository.GetAssetsByBrokerPortfolio("XPI", "Default").ToList();
                var acoes = _repository.GetAssetsByBrokerPortfolio("XPI", "Acoes");
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
                await Task.Delay(2000);
                btnCheckFIIS.IsEnabled = true;
                pnlFIIsProgress.Visibility = Visibility.Collapsed;
                pgFIIsProgress.Value = 0;
            }
        }
    }
}
