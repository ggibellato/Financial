namespace Financial.Api.DTOs;

/// <summary>
/// The active data repository configuration for both bounded contexts.
/// <para>
/// Available in every environment, because the question it answers - "is this deploy pointed at the
/// provider I think it is, and is that provider configured" - only ever gets asked about a running
/// deployment. Outside Development the paths themselves are withheld and only the
/// <c>*Configured</c> flags are populated: the API has no authentication, so anything that reaches
/// the port would otherwise learn the filesystem and credential-file layout.
/// </para>
/// </summary>
public sealed class RepositoryConfigDTO
{
    public required RepositoryContextConfigDTO Investment { get; init; }

    public required RepositoryContextConfigDTO CashFlow { get; init; }
}

/// <summary>
/// One context's repository configuration. The shape is the same in every environment so a caller
/// never has to branch on it; only the path values are withheld outside Development.
/// </summary>
public sealed class RepositoryContextConfigDTO
{
    /// <summary>The configured repository provider (e.g. "LocalJson", "GoogleDrive"). Never withheld.</summary>
    public string? Provider { get; init; }

    /// <summary>Path to the local data JSON file. Null outside Development.</summary>
    public string? DataJsonFile { get; init; }

    /// <summary>Whether a local data file path is configured. Populated in every environment.</summary>
    public bool DataJsonFileConfigured { get; init; }

    /// <summary>Path to the Google Drive service account credentials file. Null outside Development.</summary>
    public string? GoogleDriveCredentialsPath { get; init; }

    /// <summary>Whether Google Drive credentials are configured. Populated in every environment.</summary>
    public bool GoogleDriveCredentialsConfigured { get; init; }

    /// <summary>Path to the data file on Google Drive. Null outside Development.</summary>
    public string? GoogleDriveFilePath { get; init; }

    /// <summary>Whether a Google Drive file path is configured. Populated in every environment.</summary>
    public bool GoogleDriveFileConfigured { get; init; }
}
