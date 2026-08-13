namespace Financial.Api.DTOs;

/// <summary>
/// One bounded context's current persistence status.
/// </summary>
public sealed class SyncStatusDTO
{
    /// <summary>One of "Idle", "Pending", "Saving", "Failed".</summary>
    public required string State { get; init; }

    /// <summary>The triggering error's message when <see cref="State"/> is "Failed"; otherwise null.</summary>
    public string? LastError { get; init; }

    /// <summary>UTC timestamp of the last successful save, or null if none has occurred yet.</summary>
    public DateTime? LastSuccessfulSaveUtc { get; init; }
}
