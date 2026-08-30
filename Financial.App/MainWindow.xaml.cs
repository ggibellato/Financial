using Financial.Presentation.App.Components;
using Financial.Presentation.App.Properties;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.Views.Admin;
using Financial.Presentation.App.Views.CashFlow;
using Financial.Presentation.App.Views.Investment;
using System.Windows;

namespace Financial.Presentation.App
{
    public partial class MainWindow : Window
    {
        private readonly MainNavigationViewModel _navigationViewModel;
        private readonly MainNavigationViewModelHistoric _navigationViewModelHistoric;

        public MainWindow(
            DividendCheckView dividendCheckView,
            AssetPriceView assetPriceView,
            MonthlyView monthlyView,
            ReservaView reservaView,
            MensaisView mensaisView,
            ControleMaeView controleMaeView,
            InvestmentSnapshotsView investmentSnapshotsView,
            AnnualSummaryView annualSummaryView,
            Financial.Presentation.App.Views.Admin.BrokersView brokersView,
            Financial.Presentation.App.Views.Admin.PortfoliosView portfoliosView,
            MainNavigationViewModel navigationViewModel,
            MainNavigationViewModelHistoric navigationViewModelHistoric,
            SyncStatusViewModel syncStatusViewModel)
        {
            ArgumentNullException.ThrowIfNull(dividendCheckView);
            ArgumentNullException.ThrowIfNull(assetPriceView);
            ArgumentNullException.ThrowIfNull(monthlyView);
            ArgumentNullException.ThrowIfNull(reservaView);
            ArgumentNullException.ThrowIfNull(mensaisView);
            ArgumentNullException.ThrowIfNull(controleMaeView);
            ArgumentNullException.ThrowIfNull(investmentSnapshotsView);
            ArgumentNullException.ThrowIfNull(annualSummaryView);
            ArgumentNullException.ThrowIfNull(brokersView);
            ArgumentNullException.ThrowIfNull(portfoliosView);
            ArgumentNullException.ThrowIfNull(syncStatusViewModel);
            _navigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
            _navigationViewModelHistoric = navigationViewModelHistoric ?? throw new ArgumentNullException(nameof(navigationViewModelHistoric));

            InitializeComponent();

            var viewsByKey = new Dictionary<string, object>
            {
                ["active-investments"] = new NavigationView { DataContext = _navigationViewModel },
                ["historic-investments"] = new NavigationView { DataContext = _navigationViewModelHistoric },
                ["dividend-check"] = dividendCheckView,
                ["current-values"] = assetPriceView,
                ["monthly"] = monthlyView,
                ["reserva"] = reservaView,
                ["mensais"] = mensaisView,
                ["controle-mae"] = controleMaeView,
                ["investment-snapshots"] = investmentSnapshotsView,
                ["annual-summary"] = annualSummaryView,
                ["admin-assets"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Assets")),
                ["admin-brokers"] = brokersView,
                ["admin-portfolios"] = portfoliosView,
                ["admin-banks"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Banks")),
                ["admin-categories"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Categories")),
                ["admin-credit-cards"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Credit Cards")),
                ["admin-income-sources"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Income Sources")),
                ["admin-investment-accounts"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Investment Accounts")),
                ["admin-recurring-bills"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Recurring Bills")),
                ["admin-reserve-buckets"] = new AdminEntityPlaceholderView(new AdminEntityPlaceholderViewModel("Reserve Buckets")),
            };

            DataContext = new MainShellViewModel(
                initialCollapsed: Settings.Default.IsNavigationSidebarCollapsed,
                persistCollapsed: collapsed =>
                {
                    Settings.Default.IsNavigationSidebarCollapsed = collapsed;
                    Settings.Default.Save();
                },
                viewsByKey: viewsByKey,
                syncStatusViewModel: syncStatusViewModel);

            Loaded += async (s, e) =>
            {
                await Task.WhenAll(
                    _navigationViewModel.LoadNavigationTreeAsync(),
                    _navigationViewModelHistoric.LoadNavigationTreeAsync());
            };
        }
    }
}
