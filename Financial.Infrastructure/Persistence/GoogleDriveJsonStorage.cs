using Financial.Investment.Application.Interfaces;
using Financial.Infrastructure.Configuration;

namespace Financial.Infrastructure.Persistence;

public sealed class GoogleDriveJsonStorage : IJsonStorage
{
    private readonly Func<string, string> _download;
    private readonly Action<string, string> _upload;
    private readonly string _driveFilePath;

    public GoogleDriveJsonStorage(IRemoteFileClient client, string? driveFilePath)
        : this(
            (client ?? throw new ArgumentNullException(nameof(client))).DownloadFileContent,
            client.UploadFileContent,
            driveFilePath)
    {
    }

    internal GoogleDriveJsonStorage(Func<string, string> download, Action<string, string> upload, string? driveFilePath)
    {
        _download = download ?? throw new ArgumentNullException(nameof(download));
        _upload = upload ?? throw new ArgumentNullException(nameof(upload));
        _driveFilePath = ResolveDriveFilePath(driveFilePath);
    }

    public Task<string> ReadAsync() =>
        Task.Run(() => _download(_driveFilePath));

    public Task WriteAsync(string json) =>
        Task.Run(() => _upload(_driveFilePath, json));

    private static string ResolveDriveFilePath(string? driveFilePath)
    {
        if (string.IsNullOrWhiteSpace(driveFilePath))
            throw new ArgumentException(
                $"Drive file path must be configured via '{RepositoryConfigurationKeys.GoogleDriveFilePath}'.",
                nameof(driveFilePath));
        return driveFilePath;
    }
}
