using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Financial.Shared.Infrastructure.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var options = configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        if (!options.Enabled)
        {
            return services;
        }

        var endpoint = new Uri(options.Endpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddSource(
                    ObservabilitySourceNames.CashFlow,
                    ObservabilitySourceNames.Investment,
                    ObservabilitySourceNames.SharedInfrastructure,
                    ObservabilitySourceNames.App)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = endpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = endpoint));

        return services;
    }
}
