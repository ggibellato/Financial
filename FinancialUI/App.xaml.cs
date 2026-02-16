using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using FinancialModel.Application;
using FinancialModel.Infrastructure;
using FinancialUI.ViewModels;

namespace FinancialUI;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register Infrastructure services
                services.AddSingleton<IRepository>(_ =>
                    new JSONRepository(context.Configuration[JSONRepository.DataJsonPathConfigurationKey]));
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
