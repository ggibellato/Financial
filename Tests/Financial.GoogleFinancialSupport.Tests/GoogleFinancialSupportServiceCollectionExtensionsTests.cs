using Financial.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.GoogleFinancialSupport.Tests;

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
