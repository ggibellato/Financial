namespace Financial.Integrations.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>The one place the section is bound imperatively - used by both the DI and the
    /// Serilog entry points so binding behavior cannot diverge between traces and logs.</summary>
    public static ObservabilityOptions From(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var options = new ObservabilityOptions();
        Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(
            configuration.GetSection(SectionName), options);
        return options;
    }

    public bool Enabled { get; init; }
    public ObservabilityBackend Backend { get; init; } = ObservabilityBackend.Jaeger;
    public string Endpoint { get; init; } = "http://localhost:4317";
    public LangfuseOptions Langfuse { get; init; } = new();
}

public sealed class LangfuseOptions
{
    public string PublicKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}
