using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using Financial.Shared.Abstractions.Persistence;
using Google;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class GoogleService : IRemoteFileClient, IGoogleSheetsDataSource
{
    private readonly GoogleDriveClient _driveClient;
    private readonly GoogleSheetsClient _sheetsClient;

    public string FileName { get; }

    public GoogleService(string fileName, ILogger? logger = null)
    {
        FileName = fileName;
        var credentialFactory = new GoogleCredentialFactory(fileName);
        _driveClient = new GoogleDriveClient(credentialFactory, logger);
        _sheetsClient = new GoogleSheetsClient(credentialFactory, logger);
    }

    public Task<List<SpreadSheetDTO>> GetFilesNameAsync() =>
        _driveClient.GetFilesAsync();

    public Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId) =>
        _sheetsClient.GetSpreadSheetAsync(spreadSheetId);

    public Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range) =>
        _sheetsClient.GetSpreadSheetDataAsync(spreadSheetId, range);

    public string DownloadFileContent(string drivePath)
    {
        try
        {
            return _driveClient.DownloadFileContent(drivePath);
        }
        catch (GoogleApiException ex)
        {
            GoogleTransientErrorTranslator.ThrowIfTransient(ex);
            throw;
        }
    }

    public void UploadFileContent(string drivePath, string content)
    {
        try
        {
            _driveClient.UploadFileContent(drivePath, content);
        }
        catch (GoogleApiException ex)
        {
            GoogleTransientErrorTranslator.ThrowIfTransient(ex);
            throw;
        }
    }
}
