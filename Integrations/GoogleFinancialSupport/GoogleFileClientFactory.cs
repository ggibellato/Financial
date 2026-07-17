using Financial.Infrastructure.Persistence;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class GoogleFileClientFactory : IRemoteFileClientFactory
{
    public IRemoteFileClient Create(string credentialsPath) => new GoogleService(credentialsPath);
}
