using Financial.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace Financial.Infrastructure.Configuration;

public sealed class RepositoryDiagnosticsProvider : ILocalJsonRepositoryDiagnostics, IGoogleDriveRepositoryDiagnostics
{
    private readonly IConfiguration _configuration;

    public RepositoryDiagnosticsProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string? Provider => _configuration[RepositoryConfigurationKeys.Provider];
    public string? DataJsonFile => _configuration[RepositoryConfigurationKeys.LocalJsonDataFile];
    public string? GoogleDriveCredentialsPath => _configuration[RepositoryConfigurationKeys.GoogleDriveCredentialsPath];
    public string? GoogleDriveFilePath => _configuration[RepositoryConfigurationKeys.GoogleDriveFilePath];
}
