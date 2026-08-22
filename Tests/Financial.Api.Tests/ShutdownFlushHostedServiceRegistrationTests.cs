using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Infrastructure.Hosting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Financial.Api.Tests;

public class ShutdownFlushHostedServiceRegistrationTests : ApiEndpointTests
{
    [Fact]
    public void ShutdownFlushHostedService_ForCashFlowRepository_IsRegistered()
    {
        var hostedServices = Services.GetServices<IHostedService>();

        hostedServices.Should().Contain(service => service is ShutdownFlushHostedService<ICashFlowRepository>);
    }
}
