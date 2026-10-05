using Financial.Integrations.GoogleDrive;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.GoogleIntegrations.Tests;

[Trait("Category", "Unit")]
public class GoogleDriveServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGoogleDriveFileClient_RegistersRemoteFileClientFactory()
    {
        var services = new ServiceCollection();

        services.AddGoogleDriveFileClient();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRemoteFileClientFactory>()
            .Should().BeOfType<GoogleFileClientFactory>();
    }
}
