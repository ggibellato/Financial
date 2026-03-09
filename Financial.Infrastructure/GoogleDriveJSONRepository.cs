using Financial.Model;
using GoogleFinancialSupport;
using System;
using System.IO;

namespace FinancialModel.Infrastructure;

public class GoogleDriveJSONRepository : InvestmentsRepositoryBase
{
    public const string CredentialsPathConfigurationKey = "GoogleDrive:CredentialsPath";
    public const string FilePathConfigurationKey = "GoogleDrive:FilePath";
    public const string DefaultDriveFilePath = "Pessoais/Gleison/Financeiros";

    private readonly string _credentialsPath;
    private readonly string _driveFilePath;

    public GoogleDriveJSONRepository(string? credentialsPath, string? driveFilePath)
        : base(LoadModel(credentialsPath, driveFilePath, out var resolvedCredentials, out var resolvedDrivePath))
    {
        _credentialsPath = resolvedCredentials;
        _driveFilePath = resolvedDrivePath;
    }

    private static Investments LoadModel(string? credentialsPath, string? driveFilePath, out string resolvedCredentials, out string resolvedDrivePath)
    {
        resolvedCredentials = ResolveCredentialsPath(credentialsPath);
        resolvedDrivePath = ResolveDriveFilePath(driveFilePath);

        var service = new GoogleService(resolvedCredentials);
        var json = service.DownloadFileContent(resolvedDrivePath);
        return Investments.Deserialize(json);
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
