namespace Financial.Shared.Abstractions.Persistence;

public interface IRemoteFileClientFactory
{
    IRemoteFileClient Create(string credentialsPath);
}
