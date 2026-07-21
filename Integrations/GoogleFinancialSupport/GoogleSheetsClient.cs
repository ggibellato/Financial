using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using Google.Apis.Sheets.v4;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

internal sealed class GoogleSheetsClient
{
    private static readonly string[] SheetsScopes = { SheetsService.Scope.Spreadsheets };

    private readonly GoogleCredentialFactory _credentialFactory;

    internal GoogleSheetsClient(GoogleCredentialFactory credentialFactory)
    {
        _credentialFactory = credentialFactory;
    }

    internal async Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId)
    {
        return await GoogleRetryPolicy.ExecuteWithRetryAsync(async () =>
        {
            var service = CreateService();
            var request = service.Spreadsheets.Get(spreadSheetId);
            request.Fields = "sheets(properties/sheetId,properties/title,properties/tabColor)";
            var response = await request.ExecuteAsync();
            return response.Sheets
                .Select(s => new SheetDTO
                {
                    Name = s.Properties.Title,
                    Id = s.Properties.SheetId ?? 0,
                    Color = GetTabColorName(s.Properties.TabColor)
                })
                .ToList();
        });
    }

    internal async Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range)
    {
        return await GoogleRetryPolicy.ExecuteWithRetryAsync(async () =>
        {
            var service = CreateService();
            var request = service.Spreadsheets.Values.Get(spreadSheetId, range);
            request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            var response = await request.ExecuteAsync();
            return response.Values;
        });
    }

    private SheetsService CreateService()
    {
        var credential = _credentialFactory.Create(SheetsScopes);
        return new SheetsService(GoogleCredentialFactory.CreateInitializer(credential));
    }

    private static string GetTabColorName(Google.Apis.Sheets.v4.Data.Color tabColor)
    {
        if (tabColor == null)
        {
            return string.Empty;
        }

        var color = Color.FromArgb(
            (int)((tabColor.Alpha ?? 0) * 255),
            (int)((tabColor.Red ?? 0) * 255),
            (int)((tabColor.Green ?? 0) * 255),
            (int)((tabColor.Blue ?? 0) * 255));
        return color.Name;
    }
}
