namespace Financial.Infrastructure.Persistence;

public interface IRemoteFileClient
{
    string DownloadFileContent(string path);

    void UploadFileContent(string path, string content);
}
