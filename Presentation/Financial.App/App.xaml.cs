using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using Financial.Application.Interfaces;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Persistence;
using Financial.Presentation.App.ViewModels;

namespace Financial.Presentation.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IHost? AppHost { get; private set; }
        private const string RepositoryProviderConfigurationKey = "Repository:Provider";

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register Infrastructure services
                    services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
                    services.AddSingleton<IRepository>(sp =>
                    {
                        var providerValue = context.Configuration[RepositoryProviderConfigurationKey]
                            ?? nameof(RepositoryProvider.LocalJson);
                        if (!Enum.TryParse(providerValue, true, out RepositoryProvider provider))
                        {
                            throw new InvalidOperationException(
                                $"Repository provider '{providerValue}' is not supported. " +
                                $"Valid values: {string.Join(", ", Enum.GetNames<RepositoryProvider>())}.");
                        }

                        var options = new RepositorySelectionOptions(
                            provider,
                            context.Configuration[LocalJsonStorage.DataJsonFileConfigurationKey],
                            context.Configuration[GoogleDriveJsonStorage.CredentialsPathConfigurationKey],
                            context.Configuration[GoogleDriveJsonStorage.FilePathConfigurationKey]);

                        var factory = sp.GetRequiredService<IRepositoryFactory>();
                        return factory.Create(options);
                    });
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IOperationService, OperationService>();
                    services.AddSingleton<ICreditService, CreditService>();
                    services.AddSingleton<IAssetPriceService, AssetPriceService>();

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
                    $"{ex.Message}\n\nSet '{LocalJsonStorage.DataJsonFileConfigurationKey}' or place '{LocalJsonStorage.DefaultDataFileName}' in the application directory.",
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



