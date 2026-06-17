using Financial.Application.Interfaces;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using System;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Persistence;

public sealed class GoogleDriveJsonStorage : IJsonStorage
{
    public const string CredentialsPathConfigurationKey = "GoogleDrive:CredentialsPath";
    public const string FilePathConfigurationKey = "GoogleDrive:FilePath";
    public const string DefaultDriveFilePath = "Pessoais/Gleison/Financeiros";

    private readonly GoogleService _service;
    private readonly string _driveFilePath;

    public GoogleDriveJsonStorage(GoogleService service, string? driveFilePath)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _driveFilePath = ResolveDriveFilePath(driveFilePath);
    }

    public Task<string> ReadAsync() =>
        Task.Run(() => _service.DownloadFileContent(_driveFilePath));

    public Task WriteAsync(string json) =>
        Task.Run(() => _service.UploadFileContent(_driveFilePath, json));

    private static string ResolveDriveFilePath(string? driveFilePath) =>
        string.IsNullOrWhiteSpace(driveFilePath) ? DefaultDriveFilePath : driveFilePath;
}
