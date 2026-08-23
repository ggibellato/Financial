using Financial.Integrations.GoogleCore;
using Financial.Integrations.GoogleDrive.DTO;
using Financial.Shared.Abstractions.Persistence;
using Google;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleDrive;

/// <summary>
/// Google Drive half of what used to be <c>GoogleService</c>. It serves two unrelated callers:
/// the storage layer, through <see cref="IRemoteFileClient"/>, and the spreadsheet importer,
/// through <see cref="IGoogleDriveFileSource"/>. Only the former is provider-swappable.
/// </summary>
public sealed class GoogleDriveFileClient : IRemoteFileClient, IGoogleDriveFileSource
{
    private readonly GoogleDriveClient _driveClient;

    public string CredentialsPath { get; }

    public GoogleDriveFileClient(string credentialsPath, ILogger? logger = null)
    {
        CredentialsPath = credentialsPath;
        _driveClient = new GoogleDriveClient(new GoogleCredentialFactory(credentialsPath), logger);
    }

    public Task<List<SpreadSheetDTO>> GetFilesAsync() => _driveClient.GetFilesAsync();

    public string DownloadFileContent(string remotePath)
    {
        try
        {
            return _driveClient.DownloadFileContent(remotePath);
        }
        catch (GoogleApiException ex)
        {
            GoogleTransientErrorTranslator.ThrowIfTransient(ex);
            throw;
        }
    }

    public void UploadFileContent(string remotePath, string content)
    {
        try
        {
            _driveClient.UploadFileContent(remotePath, content);
        }
        catch (GoogleApiException ex)
        {
            GoogleTransientErrorTranslator.ThrowIfTransient(ex);
            throw;
        }
    }
}
