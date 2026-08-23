using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Financial.Integrations.GoogleFinancialSupport;

public sealed class GoogleFileClientFactory : IRemoteFileClientFactory
{
    private readonly ILogger<GoogleService>? _logger;

    public GoogleFileClientFactory(ILogger<GoogleService>? logger = null)
    {
        _logger = logger;
    }

    public IRemoteFileClient Create(string credentialsPath) => new GoogleService(credentialsPath, _logger);
}
