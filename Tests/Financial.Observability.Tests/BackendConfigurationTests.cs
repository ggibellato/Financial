using System.Text;
using Financial.Integrations.Observability;
using Financial.Shared.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;

namespace Financial.Observability.Tests;

/// <summary>T045: the OTLP exporter is configured per backend (plain endpoint for Jaeger,
/// Basic Auth + HTTP for Langfuse), and a misconfigured backend fails fast at startup.</summary>
public class BackendConfigurationTests
{
    [Fact]
    public void ConfigureOtlpExporter_JaegerBackend_SetsEndpointWithoutAuthHeaders()
    {
        var options = new ObservabilityOptions
        {
            Enabled = true,
            Backend = ObservabilityBackend.Jaeger,
            Endpoint = "http://localhost:4317"
        };
        var exporter = new OtlpExporterOptions();

        ObservabilityServiceCollectionExtensions.ConfigureOtlpExporter(options, exporter);

        exporter.Endpoint.Should().Be(new Uri("http://localhost:4317"));
        exporter.Protocol.Should().Be(OtlpExportProtocol.Grpc);
        exporter.Headers.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ConfigureOtlpExporter_LangfuseBackend_SetsBasicAuthHeaderAndHttpProtocol()
    {
        var options = new ObservabilityOptions
        {
            Enabled = true,
            Backend = ObservabilityBackend.Langfuse,
            Endpoint = "http://localhost:3000/api/public/otel/v1/traces",
            Langfuse = new LangfuseOptions { PublicKey = "pk-lf-test", SecretKey = "sk-lf-test" }
        };
        var exporter = new OtlpExporterOptions();

        ObservabilityServiceCollectionExtensions.ConfigureOtlpExporter(options, exporter);

        var expectedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("pk-lf-test:sk-lf-test"));
        exporter.Endpoint.Should().Be(new Uri("http://localhost:3000/api/public/otel/v1/traces"));
        exporter.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
        exporter.Headers.Should().Be($"Authorization=Basic {expectedCredentials}");
    }

    [Fact]
    public void AddObservability_LangfuseBackendWithBothKeys_RegistersTheRealTracer()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Langfuse",
            ["Observability:Endpoint"] = "http://localhost:3000/api/public/otel/v1/traces",
            ["Observability:Langfuse:PublicKey"] = "pk-lf-test",
            ["Observability:Langfuse:SecretKey"] = "sk-lf-test"
        });

        provider.GetRequiredService<ITelemetryTracer>().Should().BeOfType<OpenTelemetryTracer>();
    }

    [Theory]
    [InlineData(null, "sk-lf-test")]
    [InlineData("pk-lf-test", null)]
    [InlineData(null, null)]
    public void AddObservability_LangfuseBackendMissingAKey_FailsFastAtStartup(string? publicKey, string? secretKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Langfuse",
            ["Observability:Endpoint"] = "http://localhost:3000/api/public/otel/v1/traces",
            ["Observability:Langfuse:PublicKey"] = publicKey,
            ["Observability:Langfuse:SecretKey"] = secretKey
        };

        var act = () => BuildServiceProvider(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Langfuse*PublicKey*SecretKey*");
    }

    [Fact]
    public void AddObservability_UnrecognizedBackend_FailsFastAtStartup()
    {
        // "999" binds to an undefined enum member; a misspelled name (e.g. "Zipkin") is rejected
        // even earlier, by the configuration binder itself. Either way startup fails, never a
        // silent export-to-nowhere.
        var act = () => BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "999",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unrecognized*Backend*");
    }

    [Fact]
    public void AddObservability_MisspelledBackendName_FailsFastAtStartup()
    {
        var act = () => BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Zipkin",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddObservability_Disabled_SkipsBackendValidation()
    {
        // A disabled deployment must start even with leftover/incomplete backend settings.
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "false",
            ["Observability:Backend"] = "Langfuse"
        });

        provider.GetRequiredService<ITelemetryTracer>().Should().BeOfType<NoOpTelemetryTracer>();
    }

    private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddObservability(configuration, serviceName: "Financial.Tests");

        return services.BuildServiceProvider();
    }
}
