using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class SummaryEndpointsTests
{
    [Fact]
    public async Task GetPortfolioAssetsSummary_Returns200WithItems()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Default/assets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioAssetSummaryItemDTO>>();
        items.Should().NotBeNull();
        items.Should().NotBeEmpty();
        items![0].AssetName.Should().NotBeNullOrEmpty();
        items.Should().AllSatisfy(i =>
        {
            i.TotalBought.Should().BeGreaterThanOrEqualTo(0m);
            i.PortfolioWeight.Should().BeGreaterThanOrEqualTo(0m);
        });
    }

    [Fact]
    public async Task GetPortfolioAssetsSummary_Returns400ForWhitespaceBrokerName()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/%20/Default/assets");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task GetPortfolioAssetsSummary_Returns200WithNewFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Default/assets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioAssetSummaryItemDTO>>();
        items.Should().NotBeNull();
        items.Should().NotBeEmpty();
        items!.Should().AllSatisfy(i =>
        {
            i.TotalCredits.Should().BeGreaterThanOrEqualTo(0m);
            i.CashFlows.Should().NotBeNull();
        });
        items.Should().Contain(i => i.CashFlows.Count > 0);
    }

    [Fact]
    public async Task GetPortfolioAssetsSummary_Returns200WithCreditsAnalysisFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Default/assets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioAssetSummaryItemDTO>>();
        items.Should().NotBeNull();
        items.Should().NotBeEmpty();
        items!.Should().AllSatisfy(i =>
        {
            i.LastMonthCredits.Should().BeGreaterThanOrEqualTo(0m);
            i.CurrentMonthCredits.Should().BeGreaterThanOrEqualTo(0m);
        });
        items!.Where(i => i.LastCreditMonth != null && i.TotalInvested > 0)
            .Should().AllSatisfy(i => i.LastMonthCreditsPercent.Should().NotBeNull());
        items.Where(i => i.CreditFrequencyPerYear != null)
            .Should().AllSatisfy(i => i.EstimatedAnnualCredits.Should().NotBeNull());
    }

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
        dto.TotalInvested.Should().Be(dto.TotalBought - dto.TotalSold);
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
        dto.TotalInvested.Should().Be(dto.TotalBought - dto.TotalSold);
    }
}
