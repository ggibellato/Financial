using Financial.Presentation.App.Components;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.Views;
using System.Windows;
using System.Windows.Media;

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
            _navigationViewModel = navigationViewModel;

            InitializeComponent();

            dividendCheckTab.Content = dividendCheckView;
            assetPriceTab.Content = assetPriceView;

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
    }
}
