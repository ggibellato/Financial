using Financial.Presentation.App.Components;
using Financial.Presentation.App.Properties;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Settings;
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
            Financial.Presentation.App.Views.Investment.Dashboard.DashboardView dashboardView,
            Financial.Presentation.App.ViewModels.Investment.Dashboard.DashboardKpiTilesViewModel dashboardKpiTilesViewModel,
            Financial.Presentation.App.ViewModels.Investment.Dashboard.AllocationBreakdownViewModel allocationBreakdownViewModel,
            Financial.Presentation.App.ViewModels.Investment.Dashboard.DataQualityWarningsViewModel dataQualityWarningsViewModel,
            DividendCheckView dividendCheckView,
            AssetPriceView assetPriceView,
            MonthlyView monthlyView,
            ReservaView reservaView,
            MensaisView mensaisView,
            ControleMaeView controleMaeView,
            InvestmentSnapshotsView investmentSnapshotsView,
            AnnualSummaryView annualSummaryView,
            TaxView taxView,
            Financial.Presentation.App.Views.Admin.BrokersView brokersView,
            Financial.Presentation.App.Views.Admin.PortfoliosView portfoliosView,
            Financial.Presentation.App.Views.Admin.AssetsView assetsView,
            Financial.Presentation.App.Views.Admin.BanksView banksView,
            Financial.Presentation.App.Views.Admin.CategoriesView categoriesView,
            Financial.Presentation.App.Views.Admin.CreditCardsView creditCardsView,
            Financial.Presentation.App.Views.Admin.IncomeSourcesView incomeSourcesView,
            Financial.Presentation.App.Views.Admin.InvestmentAccountsView investmentAccountsView,
            Financial.Presentation.App.Views.Admin.ReserveBucketsView reserveBucketsView,
            Financial.Presentation.App.Views.Admin.TaxRulesView taxRulesView,
            Financial.Presentation.App.Views.Admin.RecurringBillsView recurringBillsView,
            MainNavigationViewModel navigationViewModel,
            MainNavigationViewModelHistoric navigationViewModelHistoric,
            SyncStatusViewModel syncStatusViewModel,
            PaymentDueBannerViewModel paymentDueBannerViewModel,
            ColourModeViewModel colourModeViewModel,
            Financial.Presentation.App.Views.Settings.AppearanceView appearanceView,
            Financial.Presentation.App.Views.Settings.ReportingCurrencyView reportingCurrencyView,
            Financial.Presentation.App.Views.Settings.SettingsIntegrationsView settingsIntegrationsView)
        {
            ArgumentNullException.ThrowIfNull(dashboardView);
            ArgumentNullException.ThrowIfNull(dashboardKpiTilesViewModel);
            ArgumentNullException.ThrowIfNull(dividendCheckView);
            ArgumentNullException.ThrowIfNull(assetPriceView);
            ArgumentNullException.ThrowIfNull(monthlyView);
            ArgumentNullException.ThrowIfNull(reservaView);
            ArgumentNullException.ThrowIfNull(mensaisView);
            ArgumentNullException.ThrowIfNull(controleMaeView);
            ArgumentNullException.ThrowIfNull(investmentSnapshotsView);
            ArgumentNullException.ThrowIfNull(annualSummaryView);
            ArgumentNullException.ThrowIfNull(taxView);
            ArgumentNullException.ThrowIfNull(brokersView);
            ArgumentNullException.ThrowIfNull(portfoliosView);
            ArgumentNullException.ThrowIfNull(assetsView);
            ArgumentNullException.ThrowIfNull(banksView);
            ArgumentNullException.ThrowIfNull(categoriesView);
            ArgumentNullException.ThrowIfNull(creditCardsView);
            ArgumentNullException.ThrowIfNull(incomeSourcesView);
            ArgumentNullException.ThrowIfNull(investmentAccountsView);
            ArgumentNullException.ThrowIfNull(reserveBucketsView);
            ArgumentNullException.ThrowIfNull(taxRulesView);
            ArgumentNullException.ThrowIfNull(recurringBillsView);
            ArgumentNullException.ThrowIfNull(syncStatusViewModel);
            ArgumentNullException.ThrowIfNull(paymentDueBannerViewModel);
            ArgumentNullException.ThrowIfNull(colourModeViewModel);
            ArgumentNullException.ThrowIfNull(appearanceView);
            ArgumentNullException.ThrowIfNull(reportingCurrencyView);
            ArgumentNullException.ThrowIfNull(settingsIntegrationsView);
            _navigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
            _navigationViewModelHistoric = navigationViewModelHistoric ?? throw new ArgumentNullException(nameof(navigationViewModelHistoric));

            assetPriceView.ViewModel.FetchCompleted += (_, _) =>
            {
                _navigationViewModel.ReloadSelectedNodeDetails();
                _navigationViewModelHistoric.ReloadSelectedNodeDetails();
            };

            InitializeComponent();

            // Composed by hand, not resolved from the container: both navigation view models are
            // registered Transient, so asking DI for them a second time would hand the dashboard a
            // pair of fresh trees instead of the two the visible panes below are actually bound to,
            // and every click-through would select a node nobody can see.
            var dashboardViewModel = new Financial.Presentation.App.ViewModels.Investment.Dashboard.DashboardViewModel(
                dashboardKpiTilesViewModel,
                allocationBreakdownViewModel,
                dataQualityWarningsViewModel,
                _navigationViewModel,
                _navigationViewModelHistoric);
            dashboardView.DataContext = dashboardViewModel;

            var viewsByKey = new Dictionary<string, object>
            {
                ["dashboard"] = dashboardView,
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
                ["tax"] = taxView,
                ["admin-assets"] = assetsView,
                ["admin-brokers"] = brokersView,
                ["admin-portfolios"] = portfoliosView,
                ["admin-banks"] = banksView,
                ["admin-categories"] = categoriesView,
                ["admin-credit-cards"] = creditCardsView,
                ["admin-income-sources"] = incomeSourcesView,
                ["admin-investment-accounts"] = investmentAccountsView,
                ["admin-recurring-bills"] = recurringBillsView,
                ["admin-reserve-buckets"] = reserveBucketsView,
                ["admin-tax-rules"] = taxRulesView,
                ["settings-appearance"] = appearanceView,
                ["settings-reporting-currency"] = reportingCurrencyView,
                ["settings-integrations"] = settingsIntegrationsView,
            };

            var shellViewModel = new MainShellViewModel(
                initialCollapsed: Settings.Default.IsNavigationSidebarCollapsed,
                persistCollapsed: collapsed =>
                {
                    Settings.Default.IsNavigationSidebarCollapsed = collapsed;
                    Settings.Default.Save();
                },
                viewsByKey: viewsByKey,
                syncStatusViewModel: syncStatusViewModel,
                paymentDueBannerViewModel: paymentDueBannerViewModel,
                colourModeViewModel: colourModeViewModel);

            DataContext = shellViewModel;

            dashboardViewModel.NavigateToTreeRequested += (_, scope) => shellViewModel.SelectItemCommand.Execute(
                scope == Financial.Investment.Application.Enums.InvestmentScope.Historic
                    ? "historic-investments"
                    : "active-investments");

            Loaded += async (s, e) =>
            {
                await Task.WhenAll(
                    _navigationViewModel.LoadNavigationTreeAsync(),
                    _navigationViewModelHistoric.LoadNavigationTreeAsync());
            };
        }
    }
}
