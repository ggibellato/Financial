namespace Financial.Shared.Infrastructure.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

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
