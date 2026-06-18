using Financial.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Financial.Infrastructure.Configuration;

public sealed class RepositorySettingsProvider : ILocalJsonRepositorySettings, IGoogleDriveRepositorySettings
{
    private readonly IConfiguration _configuration;

    public RepositorySettingsProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string? Provider => _configuration[RepositoryConfigurationKeys.Provider];
    public string? DataJsonFile => _configuration[RepositoryConfigurationKeys.LocalJsonDataFile];
    public string? GoogleDriveCredentialsPath => _configuration[RepositoryConfigurationKeys.GoogleDriveCredentialsPath];
    public string? GoogleDriveFilePath => _configuration[RepositoryConfigurationKeys.GoogleDriveFilePath];
}
