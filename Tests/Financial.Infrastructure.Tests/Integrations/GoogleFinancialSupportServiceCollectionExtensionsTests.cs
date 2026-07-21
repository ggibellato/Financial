using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Infrastructure.Tests.Integrations;

public class GoogleFinancialSupportServiceCollectionExtensionsTests
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
