using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.Views;
using System.Windows;

namespace Financial.Presentation.App
{
    public partial class MainWindow : Window
    {
        private readonly MainNavigationViewModel _navigationViewModel;

        public MainNavigationViewModel NavigationViewModel => _navigationViewModel;

        public MainWindow(
            DividendCheckView dividendCheckView,
            AssetPriceView assetPriceView,
            MainNavigationViewModel navigationViewModel)
        {
            ArgumentNullException.ThrowIfNull(dividendCheckView);
            ArgumentNullException.ThrowIfNull(assetPriceView);
            _navigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));

            InitializeComponent();
            dividendCheckTab.Content = dividendCheckView;
            assetPriceTab.Content = assetPriceView;

            Loaded += async (s, e) => await _navigationViewModel.LoadNavigationTreeAsync();
        }
    }
}
