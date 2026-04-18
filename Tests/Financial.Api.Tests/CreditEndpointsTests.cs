using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Financial.Api.Tests;

public class CreditEndpointsTests
{
    [Fact]
    public async Task GetCreditsByBroker_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/credits/broker/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCreditsByPortfolio_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/credits/portfolio/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits!.Length.Should().BeGreaterThan(0);
    }
}
