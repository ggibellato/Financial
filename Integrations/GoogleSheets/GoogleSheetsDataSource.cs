using Financial.Integrations.GoogleCore;
using Financial.Integrations.GoogleSheets.DTO;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleSheets;

/// <summary>
/// Google Sheets half of what used to be <c>GoogleService</c>. Nothing outside the Investment
/// spreadsheet importer consumes this - it is not part of any storage abstraction.
/// </summary>
public sealed class GoogleSheetsDataSource : IGoogleSheetsDataSource
{
    private readonly GoogleSheetsClient _sheetsClient;

    public GoogleSheetsDataSource(string credentialsPath, ILogger? logger = null)
    {
        _sheetsClient = new GoogleSheetsClient(new GoogleCredentialFactory(credentialsPath), logger);
    }

    public Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId) =>
        _sheetsClient.GetSpreadSheetAsync(spreadSheetId);

    public Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range) =>
        _sheetsClient.GetSpreadSheetDataAsync(spreadSheetId, range);
}
