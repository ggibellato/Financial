namespace Financial.Api.DTOs;

/// <summary>
/// The active data repository configuration. Only exposed in the Development environment.
/// </summary>
public sealed class RepositoryConfigDTO
{
    /// <summary>The configured repository provider (e.g. "LocalJson", "GoogleDriveJson").</summary>
    public string? Provider { get; init; }

    /// <summary>Path to the local investment data JSON file, when using a local provider.</summary>
    public string? DataJsonFile { get; init; }

    /// <summary>Path to the Google Drive service account credentials file, when using a Google Drive provider.</summary>
    public string? GoogleDriveCredentialsPath { get; init; }

    /// <summary>Path to the data file on Google Drive, when using a Google Drive provider.</summary>
    public string? GoogleDriveFilePath { get; init; }
}
