using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using FinancialModel.Application;
using FinancialModel.Infrastructure;
using FinancialUI.ViewModels;

namespace FinancialUI;

public partial class App : Application
{
    private readonly IHost _host;
    private const string RepositoryProviderConfigurationKey = "Repository:Provider";

    public App()
    {
        _host = Host.CreateDefaultBuilder()
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
                        context.Configuration[LocalJSONRepository.DataJsonPathConfigurationKey],
                        context.Configuration[GoogleDriveJSONRepository.CredentialsPathConfigurationKey],
                        context.Configuration[GoogleDriveJSONRepository.FilePathConfigurationKey]);

                    var factory = sp.GetRequiredService<IRepositoryFactory>();
                    return factory.Create(options);
                });
                services.AddSingleton<INavigationService, NavigationService>();

                // Register ViewModels
                services.AddTransient<MainNavigationViewModel>();

                // Register MainWindow
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }
}
