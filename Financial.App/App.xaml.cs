using Financial.Application.DependencyInjection;
using Financial.Infrastructure.DependencyInjection;
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
                    services.AddTransient<MainNavigationViewModel>();
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
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Missing data file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application failed to start:\n{ex.Message}",
                    "Startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
    }
}
