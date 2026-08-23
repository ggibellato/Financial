using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Financial.Integrations.GoogleDrive;

public sealed class GoogleFileClientFactory : IRemoteFileClientFactory
{
    private readonly ILogger<GoogleDriveFileClient>? _logger;

    public GoogleFileClientFactory(ILogger<GoogleDriveFileClient>? logger = null)
    {
        _logger = logger;
    }

    public IRemoteFileClient Create(string credentialsPath) => new GoogleDriveFileClient(credentialsPath, _logger);
}
