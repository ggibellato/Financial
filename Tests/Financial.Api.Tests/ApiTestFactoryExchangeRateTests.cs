using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

public class ApiTestFactoryExchangeRateTests
{
    [Fact]
    public void DefaultFactory_ResolvesTheStubExchangeRateProvider()
    {
        using var factory = new ApiTestFactory();

        var provider = factory.Services.GetRequiredService<IExchangeRateProvider>();

        provider.Should().BeOfType<StubExchangeRateProvider>();
    }
}
