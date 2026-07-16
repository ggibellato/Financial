using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class SummaryEndpointsTests
{
    [Fact]
    public async Task GetPortfolioAssetsSummary_Returns200WithAllExpectedFields()
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
            i.TotalCredits.Should().BeGreaterThanOrEqualTo(0m);
            i.CashFlows.Should().NotBeNull();
            i.LastMonthCredits.Should().BeGreaterThanOrEqualTo(0m);
            i.CurrentMonthCredits.Should().BeGreaterThanOrEqualTo(0m);
        });
        items.Should().Contain(i => i.CashFlows.Count > 0);
        items.Where(i => i.LastCreditMonth != null && i.TotalInvested > 0)
            .Should().AllSatisfy(i => i.LastMonthCreditsPercent.Should().NotBeNull());
        items.Where(i => i.CreditFrequencyPerYear != null)
            .Should().AllSatisfy(i => i.EstimatedAnnualCredits.Should().NotBeNull());
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

    [Fact]
    public async Task GetBrokerBreakdown_Returns200WithList()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/broker/XPI/breakdown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioBreakdownItemDTO>>();
        items.Should().NotBeNull();
        items!.Should().AllSatisfy(p =>
        {
            p.TotalInvested.Should().BeGreaterThan(0m);
            p.Assets.Should().NotBeEmpty();
            p.TotalInvested.Should().Be(p.Assets.Sum(a => a.TotalInvested));
            p.Assets.Should().AllSatisfy(a => a.TotalInvested.Should().BeGreaterThan(0m));
        });
    }

    [Fact]
    public async Task GetBrokerBreakdown_Returns400ForWhitespaceBrokerName()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/broker/%20/breakdown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBrokerSummary_ScopeHistoric_ReturnsRealizedFromHistoricBrokerOnly()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/broker/XPI?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto.Should().NotBeNull();
        dto!.TotalBought.Should().Be(300m);
        dto.TotalSold.Should().Be(250m);
    }

    [Fact]
    public async Task GetPortfolioSummary_ScopeHistoric_ReturnsHistoricOnly()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Uncategorized?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto.Should().NotBeNull();
        dto!.TotalBought.Should().Be(300m);
        dto.TotalSold.Should().Be(250m);
    }

    [Fact]
    public async Task GetPortfolioAssetsSummary_ScopeHistoric_ReturnsHistoricOnly()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Uncategorized/assets?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioAssetSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Should().ContainSingle(i => i.AssetName == "CLOSEDASSET");
        // CLOSEDASSET: bought 5 x 60 = 300, sold 5 x 50 = 250, no credits — RealizedGainLoss = 250 - 300 + 0 = -50
        items!.Single().RealizedGainLoss.Should().Be(-50m);
        items!.Single().PortfolioWeight.Should().Be(100m);
    }

    [Fact]
    public async Task GetPortfolioAssetsSummary_ScopeHistoric_PortfolioWeightsSumTo100()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Uncategorized/assets?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioAssetSummaryItemDTO>>();
        items.Should().NotBeNull();
        items!.Sum(i => i.PortfolioWeight).Should().Be(100m);
    }

    [Fact]
    public async Task GetPortfolioAssetsSummary_ScopeActive_PreservesNetInvestedWeightingAndNullRealizedGainLoss()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/portfolio/XPI/Default/assets?scope=active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioAssetSummaryItemDTO>>();
        items.Should().NotBeNull();
        // BCIA11: bought 10 x 100 = 1000, sold 2 x 110 = 220 — active weighting stays net invested (780), 100% of the portfolio
        var bcia11 = items!.Single(i => i.AssetName == "BCIA11");
        bcia11.TotalInvested.Should().Be(780m);
        bcia11.PortfolioWeight.Should().Be(100m);
        bcia11.RealizedGainLoss.Should().BeNull();
    }

    [Fact]
    public async Task GetBrokerBreakdown_ScopeHistoric_ReturnsHistoricOnly()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/broker/XPI/breakdown?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioBreakdownItemDTO>>();
        items.Should().NotBeNull();
        items!.Should().ContainSingle(p => p.PortfolioName == "Uncategorized");
        // CLOSEDASSET: bought 5 x 60 = 300, sold 5 x 50 = 250 — historic sizing uses gross TotalBought (300), not net invested (50)
        items!.Single().TotalInvested.Should().Be(300m);
        items!.SelectMany(p => p.Assets).Should().Contain(a => a.AssetName == "CLOSEDASSET" && a.TotalInvested == 300m);
    }

    [Fact]
    public async Task GetBrokerBreakdown_ScopeActive_PreservesNetInvestedBehavior()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/summary/broker/XPI/breakdown?scope=active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<PortfolioBreakdownItemDTO>>();
        items.Should().NotBeNull();
        // BCIA11: bought 10 x 100 = 1000, sold 2 x 110 = 220 — active sizing stays net invested (780)
        items!.SelectMany(p => p.Assets).Should().Contain(a => a.AssetName == "BCIA11" && a.TotalInvested == 780m);
    }
}
