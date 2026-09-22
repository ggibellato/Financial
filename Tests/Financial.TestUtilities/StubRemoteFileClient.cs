using Financial.Shared.Abstractions.Persistence;

namespace Financial.TestUtilities;

public sealed class StubRemoteFileClient : IRemoteFileClient
{
    private readonly Func<string> _downloadContent;

    public StubRemoteFileClient(Func<string> downloadContent)
    {
        _downloadContent = downloadContent;
    }

    public string DownloadFileContent(string path) => _downloadContent();

    public void UploadFileContent(string path, string content) => throw new NotSupportedException();
}

public sealed class StubRemoteFileClientFactory : IRemoteFileClientFactory
{
    private readonly IRemoteFileClient _client;

    public StubRemoteFileClientFactory(IRemoteFileClient client)
    {
        _client = client;
    }

    public IRemoteFileClient Create(string credentialsPath) => _client;
}
