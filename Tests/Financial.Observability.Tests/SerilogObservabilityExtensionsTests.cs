using Financial.Integrations.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Financial.Observability.Tests;

public class SerilogObservabilityExtensionsTests
{
    [Fact]
    public void WriteToObservability_WhenDisabled_DoesNotThrowAndReturnsUsableLogger()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "false"
        });

        var act = () => new LoggerConfiguration()
            .WriteToObservability(configuration)
            .CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void WriteToObservability_WhenEnabled_DoesNotThrowAndReturnsUsableLogger()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Jaeger",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });

        var act = () => new LoggerConfiguration()
            .WriteToObservability(configuration)
            .CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void WriteToObservability_WhenEnabledWithLangfuseBackend_DoesNotThrowAndReturnsUsableLogger()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Langfuse",
            ["Observability:Endpoint"] = "http://langfuse:4318",
            ["Observability:Langfuse:PublicKey"] = "pub-key",
            ["Observability:Langfuse:SecretKey"] = "secret-key"
        });

        var act = () => new LoggerConfiguration()
            .WriteToObservability(configuration)
            .CreateLogger();

        act.Should().NotThrow();
    }

    [Fact]
    public void WriteToObservability_WhenEnabled_LoggingDoesNotThrowEvenWithoutAReachableCollector()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Jaeger",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });
        using var logger = new LoggerConfiguration()
            .WriteToObservability(configuration)
            .CreateLogger();

        var act = () => logger.Information("Test log entry");

        act.Should().NotThrow();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
}
