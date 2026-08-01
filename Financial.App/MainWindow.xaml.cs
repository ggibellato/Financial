using Financial.Presentation.App.Views.CashFlow;
using Financial.Presentation.App.Views.Investment;
using System.Windows;

namespace Financial.Presentation.App
{
    public partial class MainWindow : Window
    {
        private readonly MainNavigationViewModel _navigationViewModel;
        private readonly MainNavigationViewModelHistoric _navigationViewModelHistoric;

        public MainNavigationViewModel NavigationViewModel => _navigationViewModel;
        public MainNavigationViewModelHistoric NavigationViewModelHistoric => _navigationViewModelHistoric;

        public MainWindow(
            DividendCheckView dividendCheckView,
            AssetPriceView assetPriceView,
            MonthlyView monthlyView,
            MainNavigationViewModel navigationViewModel,
            MainNavigationViewModelHistoric navigationViewModelHistoric)
        {
            ArgumentNullException.ThrowIfNull(dividendCheckView);
            ArgumentNullException.ThrowIfNull(assetPriceView);
            ArgumentNullException.ThrowIfNull(monthlyView);
            _navigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
            _navigationViewModelHistoric = navigationViewModelHistoric ?? throw new ArgumentNullException(nameof(navigationViewModelHistoric));

            InitializeComponent();
            dividendCheckTab.Content = dividendCheckView;
            assetPriceTab.Content = assetPriceView;
            monthlyTab.Content = monthlyView;

            Loaded += async (s, e) =>
            {
                await _navigationViewModel.LoadNavigationTreeAsync();
                await _navigationViewModelHistoric.LoadNavigationTreeAsync();
            };
        }
    }
}
