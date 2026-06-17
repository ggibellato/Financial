using Financial.Application.DependencyInjection;
using Financial.Infrastructure.DependencyInjection;
using Financial.Presentation.App.Options;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
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
                .ConfigureServices((context, services) =>
                {
                    services.AddFinancialApplication();
                    services.AddFinancialInfrastructure(context.Configuration);
                    services.Configure<WatchlistOptions>(context.Configuration.GetSection(WatchlistOptions.SectionName));
                    services.Configure<AssetPriceFetchOptions>(context.Configuration.GetSection(AssetPriceFetchOptions.SectionName));
                    services.AddTransient<MainNavigationViewModel>();
                    services.AddTransient<DividendCheckViewModel>();
                    services.AddTransient<AssetPriceFetchViewModel>();
                    services.AddTransient<DividendCheckView>();
                    services.AddTransient<AssetPriceView>();
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
