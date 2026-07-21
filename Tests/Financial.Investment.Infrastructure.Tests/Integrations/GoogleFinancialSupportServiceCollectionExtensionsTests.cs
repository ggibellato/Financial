using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

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
