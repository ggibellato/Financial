using Financial.Integrations.GoogleFinancialSupport.DTO;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleFinancialSupport;

internal sealed class GoogleSheetsClient
{
    private static readonly string[] SheetsScopes = { SheetsService.Scope.Spreadsheets };

    private readonly GoogleCredentialFactory _credentialFactory;
    private readonly Action<string>? _retryLog;
    private SheetsService? _service;

    internal GoogleSheetsClient(GoogleCredentialFactory credentialFactory, ILogger? logger = null)
    {
        _retryLog = logger is null ? null : message => logger.LogWarning("Google Sheets {RetryDetail}", message);
        _credentialFactory = credentialFactory;
    }

    internal async Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId)
    {
        return await GoogleRetryPolicy.ExecuteWithRetryAsync(async () =>
        {
            var service = GetService();
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
        }, logger: _retryLog);
    }

    internal async Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range)
    {
        return await GoogleRetryPolicy.ExecuteWithRetryAsync(async () =>
        {
            var service = GetService();
            var request = service.Spreadsheets.Values.Get(spreadSheetId, range);
            request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            var response = await request.ExecuteAsync();
            return response.Values;
        }, logger: _retryLog);
    }

    private SheetsService GetService() => _service ??= CreateService();

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
