using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

public class ApiTestFactoryExchangeRateTests
{
    [Fact]
    public void DefaultFactory_ResolvesDeterministicExchangeRateProvider()
    {
        using var factory = new ApiTestFactory();

        var provider = factory.Services.GetRequiredService<IExchangeRateProvider>();

        provider.Should().BeOfType<DeterministicExchangeRateProvider>();
    }
}
