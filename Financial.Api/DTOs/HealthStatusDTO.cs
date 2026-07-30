namespace Financial.Api.DTOs;

/// <summary>
/// Result of the API health check.
/// </summary>
public sealed class HealthStatusDTO
{
    /// <summary>Always "ok" when the API is reachable and responding.</summary>
    public required string Status { get; init; }
}
