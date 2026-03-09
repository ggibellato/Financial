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
using System.Linq;

namespace GoogleFinancialSupport;

public class GoogleService
{
    static readonly string[] Scopes1 = { SheetsService.Scope.Spreadsheets };
    static readonly string[] Scopes2 = { DriveService.Scope.DriveReadonly };
    private const string DefaultDataFileName = "data.json";
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

    public string DownloadFileContent(string drivePath)
    {
        if (string.IsNullOrWhiteSpace(drivePath))
        {
            throw new ArgumentException("Drive path must be provided.", nameof(drivePath));
        }

        var service = GetDriveService();
        var fileId = ResolveFileId(service, drivePath);

        using var stream = new MemoryStream();
        var request = service.Files.Get(fileId);
        request.Download(stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ResolveFileId(DriveService service, string drivePath)
    {
        var segments = drivePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException("Drive path must include at least one segment.", nameof(drivePath));
        }

        var parentId = "root";
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var file = FindFileByName(service, parentId, segment);
            if (file == null)
            {
                throw new FileNotFoundException($"Drive path segment '{segment}' not found in '{drivePath}'.");
            }

            bool isLast = i == segments.Length - 1;
            if (!isLast)
            {
                if (file.MimeType != "application/vnd.google-apps.folder")
                {
                    throw new DirectoryNotFoundException($"Drive path segment '{segment}' is not a folder in '{drivePath}'.");
                }
                parentId = file.Id;
                continue;
            }

            if (file.MimeType == "application/vnd.google-apps.folder")
            {
                parentId = file.Id;
                var dataFile = FindFileByName(service, parentId, DefaultDataFileName);
                if (dataFile == null)
                {
                    throw new FileNotFoundException(
                        $"Drive path '{drivePath}' points to a folder. '{DefaultDataFileName}' was not found inside it.");
                }
                return dataFile.Id;
            }

            return file.Id;
        }

        throw new FileNotFoundException($"Drive path '{drivePath}' not found.");
    }

    private static string EscapeDriveQuery(string value)
    {
        return value.Replace("'", "\\'");
    }

    private static Google.Apis.Drive.v3.Data.File? FindFileByName(DriveService service, string parentId, string name)
    {
        var listRequest = service.Files.List();
        listRequest.PageSize = 2;
        listRequest.Fields = "files(id, name, mimeType)";
        listRequest.Q = $"name = '{EscapeDriveQuery(name)}' and '{parentId}' in parents and trashed = false";

        var result = listRequest.Execute();
        return result.Files.FirstOrDefault();
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
