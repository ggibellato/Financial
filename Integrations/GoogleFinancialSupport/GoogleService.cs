using Financial.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using Financial.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class GoogleService : IRemoteFileClient
{
    private readonly GoogleDriveClient _driveClient;
    private readonly GoogleSheetsClient _sheetsClient;

    public string FileName { get; }

    public GoogleService(string fileName)
    {
        FileName = fileName;
        var credentialFactory = new GoogleCredentialFactory(fileName);
        _driveClient = new GoogleDriveClient(credentialFactory);
        _sheetsClient = new GoogleSheetsClient(credentialFactory);
    }

    public Task<List<SpreadSheetDTO>> GetFilesNameAsync() =>
        _driveClient.GetFilesAsync();

    public Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId) =>
        _sheetsClient.GetSpreadSheetAsync(spreadSheetId);

    public Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range) =>
        _sheetsClient.GetSpreadSheetDataAsync(spreadSheetId, range);

    public string DownloadFileContent(string drivePath) =>
        _driveClient.DownloadFileContent(drivePath);

    public void UploadFileContent(string drivePath, string content) =>
        _driveClient.UploadFileContent(drivePath, content);
}
