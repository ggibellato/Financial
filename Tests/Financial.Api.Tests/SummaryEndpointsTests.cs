using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class SummaryEndpointsTests
{
    [Fact]
    public async Task GetBrokerSummary_Returns200WithDto()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/broker/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto.Should().NotBeNull();
        dto!.TotalBought.Should().BeGreaterThanOrEqualTo(0m);
        dto.TotalSold.Should().BeGreaterThanOrEqualTo(0m);
        dto.TotalCredits.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public async Task GetPortfolioSummary_Returns200WithDto()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto.Should().NotBeNull();
        dto!.TotalBought.Should().BeGreaterThanOrEqualTo(0m);
        dto.TotalSold.Should().BeGreaterThanOrEqualTo(0m);
        dto.TotalCredits.Should().BeGreaterThanOrEqualTo(0m);
    }
}
