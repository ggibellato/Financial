using System.Text;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Financial.Integrations.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var options = ObservabilityOptions.From(configuration);
        services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));

        if (!options.Enabled)
        {
            services.AddSingleton<ITelemetryTracer>(NoOpTelemetryTracer.Instance);
            return services;
        }

        ValidateBackendConfiguration(options);

        services.AddSingleton<ITelemetryTracer, OpenTelemetryTracer>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddSource(OpenTelemetryTracer.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter => ConfigureOtlpExporter(options, exporter)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(exporter => ConfigureOtlpExporter(options, exporter)));

        return services;
    }

    /// <summary>Fails fast at startup (FR-009) instead of silently exporting nowhere: an
    /// unrecognized backend or a Langfuse selection without both keys is a configuration error
    /// the operator should see immediately, not discover from an empty trace UI.</summary>
    private static void ValidateBackendConfiguration(ObservabilityOptions options)
    {
        switch (options.Backend)
        {
            case ObservabilityBackend.Jaeger:
                break;
            case ObservabilityBackend.Langfuse:
                if (string.IsNullOrWhiteSpace(options.Langfuse.PublicKey) ||
                    string.IsNullOrWhiteSpace(options.Langfuse.SecretKey))
                {
                    throw new InvalidOperationException(
                        "Observability:Backend is 'Langfuse' but Observability:Langfuse:PublicKey and " +
                        "Observability:Langfuse:SecretKey are not both configured. Provide both keys, " +
                        "switch Observability:Backend to 'Jaeger', or set Observability:Enabled to false.");
                }

                break;
            default:
                throw new InvalidOperationException(
                    $"Unrecognized Observability:Backend value '{options.Backend}'. " +
                    "Supported backends: Jaeger, Langfuse.");
        }
    }

    /// <summary>Jaeger takes plain OTLP/gRPC at the configured endpoint. Langfuse ingests
    /// OTLP over HTTP with Basic Auth built from its public/secret key pair - the credentials
    /// go only into the exporter header, never into logs or telemetry attributes (FR-014).</summary>
    internal static void ConfigureOtlpExporter(ObservabilityOptions options, OtlpExporterOptions exporter)
    {
        exporter.Endpoint = new Uri(options.Endpoint);

        if (options.Backend == ObservabilityBackend.Langfuse)
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{options.Langfuse.PublicKey}:{options.Langfuse.SecretKey}"));
            exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
            exporter.Headers = $"Authorization=Basic {credentials}";
        }
    }
}
