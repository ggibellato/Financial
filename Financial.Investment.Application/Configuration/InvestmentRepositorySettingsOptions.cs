using Financial.Shared.Abstractions.Configuration;

namespace Financial.Investment.Application.Configuration;

public sealed class InvestmentRepositorySettingsOptions : JsonRepositorySettings
{
    public static readonly JsonRepositoryConfigurationKeys ConfigurationKeys = new("Investment");
}
