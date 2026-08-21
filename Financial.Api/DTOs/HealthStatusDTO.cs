namespace Financial.Api.DTOs;

/// <summary>
/// Result of the API health check.
/// <para>
/// The endpoint answers 200 whenever the process is serving, including when a context's storage is
/// failing - the body says so instead. It is used as a readiness probe (CI's boot loop, and any
/// container healthcheck), and failing it on a dependency would have an orchestrator restart the
/// container over, say, a Google Drive outage it cannot fix. That restart would also be the worst
/// available response: startup re-reads the document from the same failing storage.
/// </para>
/// </summary>
public sealed class HealthStatusDTO
{
    /// <summary>Always "ok" when the API is reachable and responding. Read the contexts for storage.</summary>
    public required string Status { get; init; }

    /// <summary>Per-bounded-context storage state, so one call answers "is anything wrong".</summary>
    public required HealthContextsDTO Contexts { get; init; }
}

/// <summary>Both bounded contexts' storage health.</summary>
public sealed class HealthContextsDTO
{
    public required HealthContextDTO Investment { get; init; }

    public required HealthContextDTO CashFlow { get; init; }
}

/// <summary>
/// One context's configured storage provider and the state of its most recent persistence attempt.
/// <para>
/// Reported from the repository's already-tracked sync status rather than by probing storage: the
/// document is read once at startup and held in memory, so a fresh read would prove nothing about
/// whether writes are landing, and a fresh Google Drive round-trip would put a network call on
/// every probe.
/// </para>
/// </summary>
public sealed class HealthContextDTO
{
    /// <summary>The configured repository provider (e.g. "LocalJson", "GoogleDrive").</summary>
    public string? Provider { get; init; }

    /// <summary>Idle, Pending, Saving or Failed.</summary>
    public required string Sync { get; init; }

    /// <summary>Why the last persistence attempt failed, when it did.</summary>
    public string? LastError { get; init; }

    /// <summary>When a write last reached storage, or null if none has since startup.</summary>
    public DateTime? LastSuccessfulSaveUtc { get; init; }
}
