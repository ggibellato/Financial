using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Financial.Integrations.Observability;

public static class SerilogObservabilityExtensions
{
    public static LoggerConfiguration WriteToObservability(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        var options = ObservabilityOptions.From(configuration);

        if (!options.Enabled)
        {
            return loggerConfiguration;
        }

        var settings = OtlpExporterSettingsResolver.Resolve(options);
        return loggerConfiguration.WriteTo.OpenTelemetry(otlp =>
        {
            otlp.Endpoint = settings.Endpoint;
            otlp.Protocol = settings.UseHttpProtobuf ? OtlpProtocol.HttpProtobuf : OtlpProtocol.Grpc;
            if (settings.AuthorizationHeaderValue is not null)
            {
                otlp.Headers = new Dictionary<string, string> { ["Authorization"] = settings.AuthorizationHeaderValue };
            }
        });
    }
}
