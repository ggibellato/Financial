using Financial.CashFlow.Application.DependencyInjection;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Integrations.Observability;
using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DependencyInjection;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Integrations.GoogleDrive;
using Financial.Presentation.App.Services;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.Presentation.App.Views.CashFlow;
using Financial.Presentation.App.Views.Investment;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Hosting;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.IO;
using System.Windows;

namespace Financial.Presentation.App
{
    public partial class App : System.Windows.Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .WriteTo.File(
                            Path.Combine(AppContext.BaseDirectory, "logs", "app-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 14)
                        .WriteToObservability(context.Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddObservability(context.Configuration, serviceName: "Financial.App");
                    services.AddFinancialApplication();
                    services.AddGoogleDriveFileClient();
                    services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
                    services.AddFinancialInfrastructure(context.Configuration);
                    services.AddFinancialCashFlowApplication();
                    services.AddFinancialCashFlowInfrastructure(context.Configuration);
                    services.AddHostedService<ShutdownFlushHostedService<ICashFlowRepository>>();
                    services.AddHostedService<ShutdownFlushHostedService<IInvestmentRepository>>();
                    services.Configure<WatchlistOptions>(context.Configuration.GetSection(WatchlistOptions.SectionName));
                    services.Configure<AssetPriceFetchOptions>(context.Configuration.GetSection(AssetPriceFetchOptions.SectionName));
                    services.Configure<DividendOptions>(context.Configuration.GetSection(DividendOptions.SectionName));
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddTransient<MainNavigationViewModel>();
                    services.AddTransient<MainNavigationViewModelHistoric>();
                    services.AddTransient<DividendCheckViewModel>();
                    services.AddTransient<AssetPriceFetchViewModel>(sp => new AssetPriceFetchViewModel(
                        sp.GetRequiredService<Financial.Investment.Application.Interfaces.INavigationService>(),
                        sp.GetRequiredService<Financial.Investment.Application.Interfaces.IAssetPriceLookupService>(),
                        sp.GetRequiredService<IOptions<AssetPriceFetchOptions>>(),
                        msg => MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)));
                    services.AddTransient<DividendCheckView>();
                    services.AddTransient<AssetPriceView>();

                    Func<string, bool> confirm = msg =>
                        MessageBox.Show(msg, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                    services.AddTransient<MonthlyViewModel>(sp => new MonthlyViewModel(
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IExpenseService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IIncomeService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IBankService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IIncomeSourceService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.ITitheService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.ITransferService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IBalanceAdjustmentService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.ICardStatementService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.ICreditCardService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.ICategoryService>(),
                        confirm,
                        sp.GetRequiredService<ITelemetryTracer>()));
                    services.AddTransient<MonthlyView>();
                    services.AddTransient<ReservaViewModel>(sp => new ReservaViewModel(
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IReserveService>(),
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IReserveBucketService>(),
                        confirm,
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ReservaViewModel>>()));
                    services.AddTransient<ReservaView>();
                    services.AddTransient<MensaisViewModel>(sp => new MensaisViewModel(
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IMensaisService>(),
                        confirm,
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MensaisViewModel>>()));
                    services.AddTransient<MensaisView>();
                    services.AddTransient<ControleMaeViewModel>(sp => new ControleMaeViewModel(
                        sp.GetRequiredService<Financial.CashFlow.Application.Interfaces.IControleMaeService>(),
                        confirm,
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ControleMaeViewModel>>()));
                    services.AddTransient<ControleMaeView>();
                    services.AddTransient<InvestmentSnapshotsViewModel>();
                    services.AddTransient<InvestmentSnapshotsView>();
                    services.AddTransient<AnnualSummaryViewModel>();
                    services.AddTransient<AnnualSummaryView>();
                    services.AddTransient<Financial.Presentation.App.ViewModels.Admin.BrokersViewModel>();
                    services.AddTransient<Financial.Presentation.App.Views.Admin.BrokersView>();
                    services.AddTransient<Financial.Presentation.App.ViewModels.Admin.PortfoliosViewModel>();
                    services.AddTransient<Financial.Presentation.App.Views.Admin.PortfoliosView>();
                    services.AddTransient<Financial.Presentation.App.ViewModels.Admin.AssetsViewModel>();
                    services.AddTransient<Financial.Presentation.App.Views.Admin.AssetsView>();
                    services.AddTransient<Financial.Presentation.App.ViewModels.Admin.BanksViewModel>();
                    services.AddTransient<Financial.Presentation.App.Views.Admin.BanksView>();
                    services.AddTransient<Financial.Presentation.App.ViewModels.Admin.CategoriesViewModel>();
                    services.AddTransient<Financial.Presentation.App.Views.Admin.CategoriesView>();
                    services.AddSingleton<Financial.Presentation.App.ViewModels.SyncStatusViewModel>();
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();
            try
            {
                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                ShowStartupError(ex);
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost != null)
            {
                await AppHost.StopAsync();
                AppHost.Dispose();
            }
            base.OnExit(e);
        }

        private static void ShowStartupError(Exception ex)
        {
            var (title, message) = ex is FileNotFoundException
                ? ("Missing data file", ex.Message)
                : ("Startup error", $"Application failed to start:\n{ex.Message}");

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
