using Financial.Shared.Abstractions;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Persistence;

public sealed class GoogleDriveJsonStorage : IJsonStorage
{
    private readonly Func<string, string> _download;
    private readonly Action<string, string> _upload;
    private readonly string _driveFilePath;
    private readonly ITelemetryTracer _tracer;

    public GoogleDriveJsonStorage(IRemoteFileClient client, string? driveFilePath, ITelemetryTracer? tracer = null)
        : this(
            (client ?? throw new ArgumentNullException(nameof(client))).DownloadFileContent,
            client.UploadFileContent,
            driveFilePath,
            tracer)
    {
    }

    internal GoogleDriveJsonStorage(
        Func<string, string> download,
        Action<string, string> upload,
        string? driveFilePath,
        ITelemetryTracer? tracer = null)
    {
        _download = download ?? throw new ArgumentNullException(nameof(download));
        _upload = upload ?? throw new ArgumentNullException(nameof(upload));
        _driveFilePath = ResolveDriveFilePath(driveFilePath);
        _tracer = tracer ?? NoOpTelemetryTracer.Instance;
    }

    public Task<string> ReadAsync() => Task.Run(() =>
    {
        using var span = _tracer.StartSpan("GoogleDrive.Download");
        try
        {
            var content = _download(_driveFilePath);
            span.MarkSuccess();
            return content;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    });

    public Task WriteAsync(string json) => Task.Run(() =>
    {
        using var span = _tracer.StartSpan("GoogleDrive.Upload");
        try
        {
            _upload(_driveFilePath, json);
            span.MarkSuccess();
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    });

    private static string ResolveDriveFilePath(string? driveFilePath)
    {
        if (string.IsNullOrWhiteSpace(driveFilePath))
            throw new ArgumentException(
                "Drive file path must be configured.",
                nameof(driveFilePath));
        return driveFilePath;
    }
}
