using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using FinancialModel.Application;
using FinancialModel.Infrastructure;
using SharesDividendCheck.ViewModels;

namespace SharesDividendCheck
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register Infrastructure services
                    services.AddSingleton<IRepository>(_ =>
                        new JSONRepository(context.Configuration[JSONRepository.DataJsonPathConfigurationKey]));
                    services.AddSingleton<INavigationService, NavigationService>();

                    // Register ViewModels
                    services.AddTransient<MainNavigationViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();
            try
            {
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(
                    $"{ex.Message}\n\nSet '{JSONRepository.DataJsonPathConfigurationKey}' or place '{JSONRepository.DefaultDataFileName}' in the application directory.",
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
