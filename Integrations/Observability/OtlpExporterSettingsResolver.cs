using System.Text;

namespace Financial.Integrations.Observability;

/// <summary>The one place that knows how backend selection (Jaeger vs. Langfuse) maps to OTLP
/// endpoint/protocol/auth-header settings, shared by the OpenTelemetry SDK exporter (traces/metrics)
/// and the Serilog OTLP sink (logs) so the two paths cannot drift apart again.</summary>
internal static class OtlpExporterSettingsResolver
{
    internal readonly record struct OtlpExporterSettings(string Endpoint, bool UseHttpProtobuf, string? AuthorizationHeaderValue);

    internal static OtlpExporterSettings Resolve(ObservabilityOptions options)
    {
        if (options.Backend != ObservabilityBackend.Langfuse)
        {
            return new OtlpExporterSettings(options.Endpoint, UseHttpProtobuf: false, AuthorizationHeaderValue: null);
        }

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.Langfuse.PublicKey}:{options.Langfuse.SecretKey}"));
        return new OtlpExporterSettings(options.Endpoint, UseHttpProtobuf: true, AuthorizationHeaderValue: $"Basic {credentials}");
    }
}
