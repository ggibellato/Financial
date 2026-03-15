using Financial.Application.Interfaces;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Persistence;

public sealed class GoogleDriveJsonStorage : IJsonStorage
{
    public const string CredentialsPathConfigurationKey = "GoogleDrive:CredentialsPath";
    public const string FilePathConfigurationKey = "GoogleDrive:FilePath";
    public const string DefaultDriveFilePath = "Pessoais/Gleison/Financeiros";

    private readonly string _credentialsPath;
    private readonly string _driveFilePath;

    public GoogleDriveJsonStorage(string? credentialsPath, string? driveFilePath)
    {
        _credentialsPath = ResolveCredentialsPath(credentialsPath);
        _driveFilePath = ResolveDriveFilePath(driveFilePath);
    }

    public Task<string> ReadAsync()
    {
        return Task.Run(() =>
        {
            var service = new GoogleService(_credentialsPath);
            return service.DownloadFileContent(_driveFilePath);
        });
    }

    public Task WriteAsync(string json)
    {
        return Task.Run(() =>
        {
            var service = new GoogleService(_credentialsPath);
            service.UploadFileContent(_driveFilePath, json);
        });
    }

    private static string ResolveCredentialsPath(string? credentialsPath)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file path is required. Configure '{CredentialsPathConfigurationKey}'.");
        }

        var resolvedPath = credentialsPath;
        if (!Path.IsPathRooted(resolvedPath))
        {
            resolvedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, resolvedPath));
        }

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file not found at '{resolvedPath}'. Configure '{CredentialsPathConfigurationKey}'.",
                resolvedPath);
        }

        return resolvedPath;
    }

    private static string ResolveDriveFilePath(string? driveFilePath)
    {
        return string.IsNullOrWhiteSpace(driveFilePath) ? DefaultDriveFilePath : driveFilePath;
    }
}
