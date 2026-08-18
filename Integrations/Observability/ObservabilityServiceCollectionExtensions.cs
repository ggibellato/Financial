using Financial.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Integrations.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));

        // Switching to OpenTelemetryTracer when Enabled=true is added in a later PR
        // (T021-T023). Every consumer gets a safe no-op regardless of configuration for now,
        // per FR-006a. `serviceName` is already part of the public signature both composition
        // roots call, so callers don't change again when the real wiring lands.
        services.AddSingleton<ITelemetryTracer, NoOpTelemetryTracer>();

        return services;
    }
}
