using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using GoogleFinancialSupport.DTO;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System;
using Google;
using System.Net;

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

    public async Task<List<SpreadSheetDTO>> GetFilesNameAsync()
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var service = GetDriveService();
            var request = service.Files.List();
            request.PageSize = 100;
            request.Fields = "nextPageToken, files(webViewLink, name, id)";
            var result = new List<SpreadSheetDTO>();
            var response = await request.ExecuteAsync();
            foreach (var file in response.Files)
            {
                result.Add(new SpreadSheetDTO() { Name = file.Name, Id = file.Id });
            }
            return result;
        });
    }

    public async Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetLink)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = new List<SheetDTO>();
            var service = GetSheetsService();
            var request = service.Spreadsheets.Get(spreadSheetLink);
            request.Fields = "sheets(properties/sheetId,properties/title,properties/tabColor)";
            var response = await request.ExecuteAsync();
            foreach (var sheet in response.Sheets)
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
        });
    }


    public async Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var result = new List<SheetDTO>();
            var service = GetSheetsService();
            var request = service.Spreadsheets.Values.Get(spreadSheetId, range);
            request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            var response = await request.ExecuteAsync();
            IList<IList<object>> values = response.Values;
            return values;
        });
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

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 5)
    {
        int retryCount = 0;
        int delayMs = 2000; // Start with 2 seconds

        while (true)
        {
            try
            {
                return await action();
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests && retryCount < maxRetries)
            {
                retryCount++;
                var waitTime = delayMs * (int)Math.Pow(2, retryCount - 1); // Exponential backoff
                Console.WriteLine($"Rate limit hit. Retry {retryCount}/{maxRetries}. Waiting {waitTime}ms...");
                await Task.Delay(waitTime);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new Exception($"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
            }
        }
    }
}
