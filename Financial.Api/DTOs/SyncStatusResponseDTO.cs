namespace Financial.Api.DTOs;

/// <summary>
/// Combined sync status for both bounded contexts.
/// </summary>
public sealed class SyncStatusResponseDTO
{
    public required SyncStatusDTO CashFlow { get; init; }

    public required SyncStatusDTO Investment { get; init; }
}
