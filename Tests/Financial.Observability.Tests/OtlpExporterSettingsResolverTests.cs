using System.Text;
using Financial.Integrations.Observability;
using FluentAssertions;

namespace Financial.Observability.Tests;

public class OtlpExporterSettingsResolverTests
{
    [Fact]
    public void Resolve_Jaeger_UsesGrpcWithNoAuthorizationHeader()
    {
        var options = new ObservabilityOptions
        {
            Enabled = true,
            Backend = ObservabilityBackend.Jaeger,
            Endpoint = "http://localhost:4317"
        };

        var settings = OtlpExporterSettingsResolver.Resolve(options);

        settings.Endpoint.Should().Be("http://localhost:4317");
        settings.UseHttpProtobuf.Should().BeFalse();
        settings.AuthorizationHeaderValue.Should().BeNull();
    }

    [Fact]
    public void Resolve_Langfuse_UsesHttpProtobufWithBasicAuthHeaderFromTheKeyPair()
    {
        var options = new ObservabilityOptions
        {
            Enabled = true,
            Backend = ObservabilityBackend.Langfuse,
            Endpoint = "http://langfuse:4318",
            Langfuse = new LangfuseOptions { PublicKey = "pub-key", SecretKey = "secret-key" }
        };

        var settings = OtlpExporterSettingsResolver.Resolve(options);

        settings.Endpoint.Should().Be("http://langfuse:4318");
        settings.UseHttpProtobuf.Should().BeTrue();
        settings.AuthorizationHeaderValue.Should().StartWith("Basic ");

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(settings.AuthorizationHeaderValue!["Basic ".Length..]));
        decoded.Should().Be("pub-key:secret-key");
    }
}
