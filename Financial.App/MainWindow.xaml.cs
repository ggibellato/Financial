using Financial.Presentation.App.Components;
using Financial.Presentation.App.Properties;
using Financial.Presentation.App.ViewModels;
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
