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

        return loggerConfiguration.WriteTo.OpenTelemetry(otlp =>
        {
            otlp.Endpoint = options.Endpoint;
            otlp.Protocol = OtlpProtocol.Grpc;
        });
    }
}
