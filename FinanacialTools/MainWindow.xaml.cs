using FinanacialTools;
using Financial.Common;
using Financial.Model;
using FinancialModel.Application;
using FinancialModel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;
using SharesDividendCheck.Components;
using SharesDividendCheck.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebPageParser;

namespace SharesDividendCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IRepository _repository = new JSONRepository();
        private readonly MainNavigationViewModel _navigationViewModel;

        public MainNavigationViewModel NavigationViewModel => _navigationViewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize navigation ViewModel from DI
            _navigationViewModel = App.AppHost!.Services.GetRequiredService<MainNavigationViewModel>();
            
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
            
            // Load navigation tree asynchronously
            Loaded += async (s, e) => 
            {
                // Ensure DataContext is set before loading
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

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            var value = GoogleFinance.GetFinancialInfo("BVMF", txtTicker.Text);
            var dividends = DadosMercadoDividend.GetDividendInfo(txtTicker.Text);
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
            var priceDiscountPer = (1 - value.Price / priceMax)*100;

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
                    dataGridColumn.Binding.StringFormat = "dd/MM/yyyy";
                }
            }
        }

        private async void btnCheckFIIS_Click(object sender, RoutedEventArgs e)
        {
            // Disable button and show progress
            btnCheckFIIS.IsEnabled = false;
            pnlFIIsProgress.Visibility = Visibility.Visible;
            fiisPriceDataGrid.ItemsSource = null;

            try
            {
                var assets = _repository.GetAssetsByBrokerPortifolio("XPI", "Default").ToList();
                var acoes = _repository.GetAssetsByBrokerPortifolio("XPI", "Acoes");
                assets.AddRange(acoes);

                var list = new List<AssetValue>();
                var totalAssets = assets.Count;
                var currentAsset = 0;

                foreach (var asset in assets)
                {
                    currentAsset++;
                    
                    // Update progress
                    var progressPercent = (currentAsset * 100.0) / totalAssets;
                    pgFIIsProgress.Value = progressPercent;
                    lblFIIsProgress.Text = $"Fetching {currentAsset} of {totalAssets}: {asset.Ticker}...";

                    // Force UI update
                    await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                    // Fetch price asynchronously (wrapped in Task.Run to avoid blocking)
                    var value = await Task.Run(() => GoogleFinance.GetFinancialInfo(asset.Exchange, asset.Ticker));
                    list.Add(value);

                    // Update DataGrid progressively to show results as they come
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
                // Re-enable button after a short delay
                await Task.Delay(2000);
                btnCheckFIIS.IsEnabled = true;
                pnlFIIsProgress.Visibility = Visibility.Collapsed;
                pgFIIsProgress.Value = 0;
            }
        }
    }
}
