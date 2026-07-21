namespace Financial.Shared.Infrastructure.Persistence;

public interface IRemoteFileClientFactory
{
    IRemoteFileClient Create(string credentialsPath);
}
