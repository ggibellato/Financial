using Financial.Shared.Abstractions.Configuration;

namespace Financial.CashFlow.Application.Configuration;

public sealed class CashFlowRepositorySettingsOptions : JsonRepositorySettings
{
    public static readonly JsonRepositoryConfigurationKeys ConfigurationKeys = new("CashFlow");
}
