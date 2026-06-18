using Financial.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal sealed class GoogleDriveClient
{
    private static readonly string[] ReadOnlyScopes = { DriveService.Scope.DriveReadonly };
    private static readonly string[] ReadWriteScopes = { DriveService.Scope.Drive };
    private const string ShortcutMimeType = "application/vnd.google-apps.shortcut";

    private readonly GoogleCredentialFactory _credentialFactory;

    internal GoogleDriveClient(GoogleCredentialFactory credentialFactory)
    {
        _credentialFactory = credentialFactory;
    }

    internal async Task<List<SpreadSheetDTO>> GetFilesAsync()
    {
        return await GoogleRetryPolicy.ExecuteWithRetryAsync(async () =>
        {
            var service = CreateService(ReadOnlyScopes);
            var request = service.Files.List();
            request.PageSize = 100;
            request.Fields = "nextPageToken, files(webViewLink, name, id)";
            var response = await request.ExecuteAsync();
            return response.Files
                .Select(f => new SpreadSheetDTO { Name = f.Name, Id = f.Id })
                .ToList();
        });
    }

    internal string DownloadFileContent(string drivePath)
    {
        if (string.IsNullOrWhiteSpace(drivePath))
        {
            throw new ArgumentException("Drive path must be provided.", nameof(drivePath));
        }

        var service = CreateService(ReadOnlyScopes);
        var fileId = ResolveFileId(service, drivePath);

        using var stream = new MemoryStream();
        service.Files.Get(fileId).Download(stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal void UploadFileContent(string drivePath, string content)
    {
        if (string.IsNullOrWhiteSpace(drivePath))
        {
            throw new ArgumentException("Drive path must be provided.", nameof(drivePath));
        }

        var service = CreateService(ReadWriteScopes);
        var fileId = ResolveFileId(service, drivePath);

        var payload = Encoding.UTF8.GetBytes(content ?? string.Empty);
        using var stream = new MemoryStream(payload);
        var request = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId, stream, "application/json");
        var result = request.Upload();
        if (result.Status != UploadStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Failed to upload file to '{drivePath}' (status: {result.Status}).",
                result.Exception);
        }
    }

    private DriveService CreateService(string[] scopes)
    {
        var credential = _credentialFactory.Create(scopes);
        return new DriveService(GoogleCredentialFactory.CreateInitializer(credential));
    }

    private static string ResolveFileId(DriveService service, string drivePath)
    {
        var segments = drivePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException("Drive path must include at least one segment.", nameof(drivePath));
        }

        var segment = segments.Last();
        var file = FindFileByName(service, segment)
            ?? throw new FileNotFoundException($"Drive path segment '{segment}' not found in '{drivePath}'.");

        return ResolveShortcutTargetId(file);
    }

    private static Google.Apis.Drive.v3.Data.File FindFileByName(DriveService service, string name)
    {
        var listRequest = service.Files.List();
        listRequest.PageSize = 10;
        listRequest.Fields = "files(id, name, mimeType, shortcutDetails(targetId, targetMimeType))";
        listRequest.Q = $"name = '{EscapeQuery(name)}' and trashed = false";
        listRequest.SupportsAllDrives = true;
        listRequest.IncludeItemsFromAllDrives = true;
        listRequest.Spaces = "drive";

        var result = listRequest.Execute();
        if (result.Files.Count > 1)
        {
            throw new InvalidOperationException($"Multiple files '{name}' found when only one was expected.");
        }

        return result.Files.FirstOrDefault();
    }

    private static string ResolveShortcutTargetId(Google.Apis.Drive.v3.Data.File file)
    {
        if (file.MimeType == ShortcutMimeType && !string.IsNullOrWhiteSpace(file.ShortcutDetails?.TargetId))
        {
            return file.ShortcutDetails.TargetId;
        }

        return file.Id;
    }

    private static string EscapeQuery(string value) => value.Replace("'", "\\'");
}
