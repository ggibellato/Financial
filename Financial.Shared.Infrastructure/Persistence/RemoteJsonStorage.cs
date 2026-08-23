using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Persistence;

public sealed class RemoteJsonStorage : IJsonStorage
{
    private readonly Func<string, string> _download;
    private readonly Action<string, string> _upload;
    private readonly string _remoteFilePath;
    private readonly ITelemetryTracer _tracer;

    public RemoteJsonStorage(IRemoteFileClient client, string? remoteFilePath, ITelemetryTracer? tracer = null)
        : this(
            (client ?? throw new ArgumentNullException(nameof(client))).DownloadFileContent,
            client.UploadFileContent,
            remoteFilePath,
            tracer)
    {
    }

    internal RemoteJsonStorage(
        Func<string, string> download,
        Action<string, string> upload,
        string? remoteFilePath,
        ITelemetryTracer? tracer = null)
    {
        _download = download ?? throw new ArgumentNullException(nameof(download));
        _upload = upload ?? throw new ArgumentNullException(nameof(upload));
        _remoteFilePath = ResolveRemoteFilePath(remoteFilePath);
        _tracer = tracer ?? NoOpTelemetryTracer.Instance;
    }

    public Task<string> ReadAsync() => Task.Run(() =>
    {
        using var span = _tracer.StartSpan("RemoteStorage.Download");
        try
        {
            var content = _download(_remoteFilePath);
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
        using var span = _tracer.StartSpan("RemoteStorage.Upload");
        try
        {
            _upload(_remoteFilePath, json);
            span.MarkSuccess();
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    });

    private static string ResolveRemoteFilePath(string? remoteFilePath)
    {
        if (string.IsNullOrWhiteSpace(remoteFilePath))
            throw new ArgumentException(
                "Remote file path must be configured.",
                nameof(remoteFilePath));
        return remoteFilePath;
    }
}
