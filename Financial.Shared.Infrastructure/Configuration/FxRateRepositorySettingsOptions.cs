using Financial.Shared.Abstractions.Configuration;

namespace Financial.Shared.Infrastructure.Configuration;

public sealed class FxRateRepositorySettingsOptions : JsonRepositorySettings
{
    public static readonly JsonRepositoryConfigurationKeys ConfigurationKeys = new("FxRates");
}
