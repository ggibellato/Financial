using Financial.Shared.Abstractions.Configuration;
using FluentAssertions;

namespace Financial.Shared.Abstractions.Tests.Configuration;

[Trait("Category", "Unit")]
public class JsonRepositorySettingsTests
{
    private sealed class TestRepositorySettings : JsonRepositorySettings;

    [Fact]
    public void Load_ReadsTheSectionScopedKeysEachContextHasAlwaysUsed()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["CashFlow:Repository:Provider"] = "GoogleDriveJson",
            ["CashFlow:DataJsonFile"] = "data/data-cashflow.json",
            ["CashFlow:GoogleDrive:CredentialsPath"] = "secrets/credentials.json",
            ["CashFlow:GoogleDrive:FilePath"] = "Financial/data-cashflow.json"
        };
        var settings = new TestRepositorySettings();

        settings.Load(new JsonRepositoryConfigurationKeys("CashFlow"), key => configuration.GetValueOrDefault(key));

        settings.Provider.Should().Be("GoogleDriveJson");
        settings.DataJsonFile.Should().Be("data/data-cashflow.json");
        settings.GoogleDriveCredentialsPath.Should().Be("secrets/credentials.json");
        settings.GoogleDriveFilePath.Should().Be("Financial/data-cashflow.json");
    }

    [Fact]
    public void ToSelection_NoProviderConfigured_DefaultsToLocalJson()
    {
        var settings = new TestRepositorySettings { DataJsonFile = "data/data-investment.json" };

        var selection = settings.ToSelection();

        selection.Should().Be(new JsonRepositorySelection(
            JsonRepositoryProvider.LocalJson, "data/data-investment.json", null, null));
    }

    [Fact]
    public void ToSelection_ProviderInAnyCase_ResolvesToTheMatchingProvider()
    {
        var settings = new TestRepositorySettings
        {
            Provider = "googledrivejson",
            GoogleDriveCredentialsPath = "credentials.json",
            GoogleDriveFilePath = "Financial/data-fx-rates.json"
        };

        var selection = settings.ToSelection();

        selection.Should().Be(new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson, null, "credentials.json", "Financial/data-fx-rates.json"));
    }

    [Fact]
    public void ToSelection_UnsupportedProvider_ThrowsNamingTheValidProviders()
    {
        var settings = new TestRepositorySettings { Provider = "NotARealProvider" };

        var act = () => settings.ToSelection();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Repository provider 'NotARealProvider' is not supported. Valid values: LocalJson, GoogleDriveJson.");
    }
}
