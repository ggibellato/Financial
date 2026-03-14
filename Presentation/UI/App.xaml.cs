using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using Financial.Application.Interfaces;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Persistence;
using Financial.Presentation.UI.ViewModels;

namespace Financial.Presentation.UI;

public partial class App : System.Windows.Application
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
                        context.Configuration[LocalJsonStorage.DataJsonFileConfigurationKey],
                        context.Configuration[GoogleDriveJsonStorage.CredentialsPathConfigurationKey],
                        context.Configuration[GoogleDriveJsonStorage.FilePathConfigurationKey]);

                    var factory = sp.GetRequiredService<IRepositoryFactory>();
                    return factory.Create(options);
                });
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IOperationService, OperationService>();
                services.AddSingleton<ICreditService, CreditService>();

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


