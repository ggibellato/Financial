using Financial.Integrations.Observability;
using Financial.Shared.Abstractions.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financial.Observability.Tests;

public class ObservabilityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddObservability_BindsObservabilityOptionsFromConfiguration_WithDefaults()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>());

        var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        options.Enabled.Should().BeFalse();
        options.Backend.Should().Be(ObservabilityBackend.Jaeger);
        options.Endpoint.Should().Be("http://localhost:4317");
    }

    [Fact]
    public void AddObservability_BindsObservabilityOptionsFromConfiguration_WithExplicitValues()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Langfuse",
            ["Observability:Endpoint"] = "http://langfuse:4318",
            ["Observability:Langfuse:PublicKey"] = "pub-key",
            ["Observability:Langfuse:SecretKey"] = "secret-key"
        });

        var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.Backend.Should().Be(ObservabilityBackend.Langfuse);
        options.Endpoint.Should().Be("http://langfuse:4318");
        options.Langfuse.PublicKey.Should().Be("pub-key");
        options.Langfuse.SecretKey.Should().Be("secret-key");
    }

    [Fact]
    public void AddObservability_AlwaysRegistersATelemetryTracer()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>());

        var tracer = provider.GetService<ITelemetryTracer>();

        tracer.Should().NotBeNull();
    }

    [Fact]
    public void AddObservability_StartSpanNeverReturnsNullOrThrows()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "false"
        });
        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        var act = () => tracer.StartSpan("Test.Operation");

        act.Should().NotThrow();
        act().Should().NotBeNull();
    }

    [Fact]
    public void AddObservability_SpanDisposeSetAttributeAndRecordExceptionNeverThrow()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>());
        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        using var span = tracer.StartSpan("Test.Operation");
        var act = () =>
        {
            span.SetAttribute("key", "value");
            span.RecordException(new InvalidOperationException("boom"));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void AddObservability_WhenDisabled_RegistersNoOpTelemetryTracer()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "false"
        });

        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        tracer.Should().BeOfType<NoOpTelemetryTracer>();
    }

    [Fact]
    public void AddObservability_WhenEnabled_RegistersOpenTelemetryTracer()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Jaeger",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });

        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        tracer.Should().BeOfType<OpenTelemetryTracer>();
    }

    [Fact]
    public void AddObservability_WhenEnabledWithLangfuseBackend_RegistersOpenTelemetryTracer()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Langfuse",
            ["Observability:Endpoint"] = "http://langfuse:4318",
            ["Observability:Langfuse:PublicKey"] = "pub-key",
            ["Observability:Langfuse:SecretKey"] = "secret-key"
        });

        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        tracer.Should().BeOfType<OpenTelemetryTracer>();
    }

    [Fact]
    public void AddObservability_WhenEnabled_StartSpanNeverReturnsNullOrThrows_EvenWithoutAListener()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Jaeger",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });
        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        var act = () => tracer.StartSpan("Test.Operation");

        act.Should().NotThrow();
        act().Should().NotBeNull();
    }

    [Fact]
    public void AddObservability_WhenEnabled_SpanDisposeSetAttributeAndRecordExceptionNeverThrow()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Jaeger",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });
        var tracer = provider.GetRequiredService<ITelemetryTracer>();

        using var span = tracer.StartSpan("Test.Operation");
        var act = () =>
        {
            span.SetAttribute("key", "value");
            span.RecordException(new InvalidOperationException("boom"));
        };

        act.Should().NotThrow();
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
