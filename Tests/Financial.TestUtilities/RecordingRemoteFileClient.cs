using Financial.Shared.Abstractions.Persistence;

namespace Financial.TestUtilities;

public sealed class RecordingRemoteFileClient : IRemoteFileClient
{
    private readonly Func<string> _downloadContent;

    public RecordingRemoteFileClient(Func<string> downloadContent)
    {
        _downloadContent = downloadContent;
    }

    public int UploadCallCount { get; private set; }

    public string? LastUploadedContent { get; private set; }

    public string DownloadFileContent(string path) => _downloadContent();

    public void UploadFileContent(string path, string content)
    {
        UploadCallCount++;
        LastUploadedContent = content;
    }
}
