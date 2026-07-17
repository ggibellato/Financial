namespace Financial.Infrastructure.Persistence;

public interface IRemoteFileClientFactory
{
    IRemoteFileClient Create(string credentialsPath);
}
