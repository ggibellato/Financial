using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.CashFlow.Infrastructure.Services;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Financial.CashFlow.Infrastructure.DependencyInjection;

public static class CashFlowInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCashFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CashFlowRepositorySettingsOptions>(options =>
            options.Load(CashFlowRepositorySettingsOptions.ConfigurationKeys, key => configuration[key]));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICashFlowSerializer, CashFlowSerializerAdapter>();
        services.AddSingleton<ICashFlowRepository>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CashFlowRepositorySettingsOptions>>().Value.ToSelection();
            return new CashFlowRepositoryFactory(
                sp.GetRequiredService<ICashFlowSerializer>(),
                sp.GetRequiredService<IJsonStorageFactory>()).Create(options);
        });

        services.Configure<GoogleCalendarSettingsOptions>(options =>
        {
            options.ClientId = configuration[CashFlowGoogleCalendarConfigurationKeys.ClientId];
            options.ClientSecret = configuration[CashFlowGoogleCalendarConfigurationKeys.ClientSecret];
            options.RedirectUri = configuration[CashFlowGoogleCalendarConfigurationKeys.RedirectUri];
            options.CredentialsPath = configuration[CashFlowGoogleCalendarConfigurationKeys.CredentialsPath];
        });
        services.AddSingleton<ICalendarConnectionStore>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<GoogleCalendarSettingsOptions>>().Value;
            return new CalendarConnectionStore(settings.CredentialsPath);
        });
        services.AddSingleton<ICalendarProvider, GoogleCalendarProviderAdapter>();

        return services;
    }
}
