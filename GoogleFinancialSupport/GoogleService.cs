using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using GoogleFinancialSupport.DTO;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace GoogleFinancialSupport;

public class GoogleService
{
    static readonly string[] Scopes1 = { SheetsService.Scope.Spreadsheets };
    static readonly string[] Scopes2 = { DriveService.Scope.DriveReadonly };
    public string FileName { get; }

    public GoogleService(string fileName)
    {
        FileName = fileName;
    }

    public List<SpreadSheetDTO> GetFilesName()
    {
        var service = GetDriveService();
        var request = service.Files.List();
        request.PageSize = 100;
        request.Fields = "nextPageToken, files(webViewLink, name, id)";
        var result = new List<SpreadSheetDTO>();
        foreach (var file in request.Execute().Files)
        {
            result.Add(new SpreadSheetDTO() { Name = file.Name, Id = file.Id });
        }
        return result;
    }

    public List<SheetDTO> GetSpreadSheet(string spreadSheetLink)
    {
        var result = new List<SheetDTO>();
        var service = GetSheetsService();
        var request = service.Spreadsheets.Get(spreadSheetLink);
        request.Fields = "sheets(properties/sheetId,properties/title,properties/tabColor)";
        foreach (var sheet in request.Execute().Sheets)
        {
            string nearestColor = "";
            var tabColor = sheet.Properties.TabColor;
            if (tabColor != null)
            {
                Color color = Color.FromArgb((int)((tabColor.Alpha ?? 0)  * 255), (int)((tabColor.Red ?? 0) * 255), (int)((tabColor.Green ?? 0) * 255), (int)((tabColor.Blue ?? 0) * 255));
                nearestColor = color.Name;
            }
            result.Add(new SheetDTO() { Name = sheet.Properties.Title, Id = sheet.Properties.SheetId ?? 0, Color = nearestColor });
        }
        return result;
    }


    public IList<IList<object>> GetSpreadSheetData(string spreadSheetId, string range)
    {
        var result = new List<SheetDTO>();
        var service = GetSheetsService();
        var request = service.Spreadsheets.Values.Get(spreadSheetId, range);
        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
        var response = request.Execute();
        IList<IList<object>> values = response.Values;
        return values;
    }

    private DriveService GetDriveService()
    {
        GoogleCredential credential;
        //Reading Credentials File...
        using (var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(Scopes2);
        }
        // Creating Google Sheets API service...
        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Financial",
        });
    }

    private SheetsService GetSheetsService()
    {
        GoogleCredential credential;
        //Reading Credentials File...
        using (var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(Scopes1);
        }
        // Creating Google Sheets API service...
        return new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Financial",
        });
    }
}
