using Financial.Investment.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class PortfolioEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetPortfolios_ReturnsSeededPortfolioWithBrokerAndAssetCount()
    {
        var response = await Client.GetAsync("/api/v1/financial/portfolios");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var portfolios = await response.Content.ReadFromJsonAsync<List<PortfolioDTO>>();
        portfolios.Should().ContainSingle(p => p.Name == "Default" && p.BrokerName == "XPI" && p.BrokerStatus == "Active" && p.AssetCount == 1);
    }

    [Fact]
    public async Task CreatePortfolio_UnderActiveBroker_ReturnsOkWithPortfolio()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "XPI", Name = "TestPortfolioA" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDTO>();
        portfolio!.Name.Should().Be("TestPortfolioA");
        portfolio.BrokerName.Should().Be("XPI");
        portfolio.BrokerStatus.Should().Be("Active");
        portfolio.AssetCount.Should().Be(0);
    }

    [Fact]
    public async Task CreatePortfolio_UnknownBroker_ReturnsNotFound()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "NoSuchBroker", Name = "TestPortfolioA" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePortfolio_DuplicateNameUnderSameBroker_ReturnsConflict()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "XPI", Name = "TestPortfolioA" });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "XPI", Name = "TestPortfolioA" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TestPortfolioA").And.Contain("already has a portfolio named");
    }

    [Fact]
    public async Task CreatePortfolio_SameNameUnderADifferentBroker_Succeeds()
    {
        // "Default" already exists under XPI (seeded); the same name under a different broker must
        // not collide - uniqueness is per-broker, not global.
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", new BrokerCreateDTO { Name = "TestBrokerA", Currency = "BRL" });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "TestBrokerA", Name = "Default" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePortfolio_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "XPI", Name = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePortfolio_ValidRequest_PersistsNewName()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "XPI", Name = "TestPortfolioA" });

        var response = await Client.PutAsJsonAsync("/api/v1/financial/portfolios/XPI/TestPortfolioA", new PortfolioUpdateDTO { Name = "TestPortfolioRenamed" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDTO>();
        portfolio!.Name.Should().Be("TestPortfolioRenamed");
    }

    [Fact]
    public async Task UpdatePortfolio_UnknownPortfolio_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/portfolios/XPI/NoSuchPortfolio", new PortfolioUpdateDTO { Name = "New Name" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePortfolio_NameCollidesWithAnotherPortfolioUnderSameBroker_ReturnsConflict()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO { BrokerName = "XPI", Name = "TestPortfolioA" });

        var response = await Client.PutAsJsonAsync("/api/v1/financial/portfolios/XPI/TestPortfolioA", new PortfolioUpdateDTO { Name = "Default" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_AfterAMoveEmptiesIt_RemovesIt()
    {
        // The sequence the feature exists for: the last asset leaves, then the portfolio can go.
        await MoveAssetAwayAsync();

        var response = await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tree = await Client.GetStringAsync("/api/v1/financial/navigation/tree");
        tree.Should().NotContain("Default");
        tree.Should().Contain("ISA");
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_WhileItStillHoldsAssets_ReturnsConflictExplainingWhy()
    {
        var response = await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("Only an empty portfolio");
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_LeavesTheAssetsAloneWhenRefused()
    {
        await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/Default");

        var asset = await Client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");
        asset.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_WhenUnknown_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/NoSuchPortfolio");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_WhenTheBrokerIsUnknown_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/v1/financial/portfolios/NoSuchBroker/Default");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_InHistoricScope_RemovesTheHistoricPortfolio()
    {
        // The same endpoint serves both scopes; without the query string it would look in Active.
        await Client.PostAsJsonAsync("/api/v1/financial/assets/move", new MoveAssetRequestDTO
        {
            BrokerName = "XPI",
            Scope = "historic",
            SourcePortfolioName = "Uncategorized",
            AssetName = "CLOSEDASSET",
            DestinationPortfolioName = "Closed 2024"
        });

        var response = await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/Uncategorized?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var historic = await Client.GetStringAsync("/api/v1/financial/navigation/tree?scope=historic");
        historic.Should().NotContain("Uncategorized");
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_WithoutAScope_DoesNotReachTheHistoricPortfolio()
    {
        var response = await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/Uncategorized");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmptyPortfolio_Persists()
    {
        await MoveAssetAwayAsync();
        await Client.DeleteAsync("/api/v1/financial/portfolios/XPI/Default");

        // Same host, fresh read: the deletion is in the document, not just in one response.
        var tree = await Client.GetStringAsync("/api/v1/financial/navigation/tree");
        tree.Should().NotContain("Default");
    }

    /// <summary>Empties "Default" by moving its only asset into a new portfolio.</summary>
    private async Task MoveAssetAwayAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", new MoveAssetRequestDTO
        {
            BrokerName = "XPI",
            Scope = "active",
            SourcePortfolioName = "Default",
            AssetName = "BCIA11",
            DestinationPortfolioName = "ISA"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
