using Financial.Shared.Infrastructure.Persistence;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class GoogleFileClientFactory : IRemoteFileClientFactory
{
    public IRemoteFileClient Create(string credentialsPath) => new GoogleService(credentialsPath);
}
