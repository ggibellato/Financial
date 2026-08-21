using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class AssetEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetAssetDetails_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Name.Should().Be("BCIA11");
        asset.BrokerName.Should().Be("XPI");
        asset.PortfolioName.Should().Be("Default");
    }

    [Fact]
    public async Task GetAssetDetails_ScopeHistoric_ResolvesHistoricAsset()
    {
        var response = await Client.GetAsync("/api/v1/financial/assets/XPI/Uncategorized/CLOSEDASSET?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Name.Should().Be("CLOSEDASSET");
        asset.PositionType.Should().Be(PositionType.Flat);
    }

    [Fact]
    public async Task GetAssetDetails_ScopeActive_HistoricAssetNotFound()
    {
        var response = await Client.GetAsync("/api/v1/financial/assets/XPI/Uncategorized/CLOSEDASSET?scope=active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveAsset_ToAPortfolioThatDoesNotExist_CreatesItAndMovesTheAsset()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", MoveRequest(destination: "ISA"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var moved = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        moved!.PortfolioName.Should().Be("ISA");

        var atDestination = await Client.GetAsync("/api/v1/financial/assets/XPI/ISA/BCIA11");
        atDestination.StatusCode.Should().Be(HttpStatusCode.OK);

        var gone = await Client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveAsset_ToAnExistingPortfolio_MovesTheAsset()
    {
        // "Default" still exists after the first move, so moving back exercises the existing-destination path.
        await Client.PostAsJsonAsync("/api/v1/financial/assets/move", MoveRequest(destination: "ISA"));

        var response = await Client.PostAsJsonAsync(
            "/api/v1/financial/assets/move",
            MoveRequest(source: "ISA", destination: "Default"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var back = await Client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");
        back.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MoveAsset_LeavesEveryFigureExactlyAsItWas()
    {
        var before = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/BCIA11");

        await Client.PostAsJsonAsync("/api/v1/financial/assets/move", MoveRequest(destination: "ISA"));

        var after = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/ISA/BCIA11");

        using (new AssertionScope())
        {
            after!.Quantity.Should().Be(before!.Quantity);
            after.AveragePrice.Should().Be(before.AveragePrice);
            after.Transactions.Should().HaveCount(before.Transactions.Count);
            after.Credits.Should().HaveCount(before.Credits.Count);
            after.ISIN.Should().Be(before.ISIN);
            after.Ticker.Should().Be(before.Ticker);
        }
    }

    [Fact]
    public async Task MoveAsset_ReturnsTheMovedAssetInTheShapeTheFrontendReads()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", MoveRequest(destination: "ISA"));

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using (new AssertionScope())
        {
            doc.RootElement.GetProperty("name").GetString().Should().Be("BCIA11");
            doc.RootElement.GetProperty("portfolioName").GetString().Should().Be("ISA");
        }
    }

    [Fact]
    public async Task MoveAsset_ToTheSamePortfolio_ReturnsConflictExplainingWhy()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", MoveRequest(destination: "Default"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("Default");
    }

    [Fact]
    public async Task MoveAsset_WithABlankDestinationName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", MoveRequest(destination: "   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MoveAsset_WithAnUnknownAsset_ReturnsNotFoundAndCreatesNoPortfolio()
    {
        var request = MoveRequest(destination: "ISA");
        request.AssetName = "NOSUCHASSET";

        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var tree = await Client.GetStringAsync("/api/v1/financial/navigation/tree");
        tree.Should().NotContain("ISA", "a refused move must not leave a portfolio behind");
    }

    [Fact]
    public async Task MoveAsset_WithAnUnknownBroker_ReturnsNotFound()
    {
        var request = MoveRequest(destination: "ISA");
        request.BrokerName = "NoSuchBroker";

        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveAsset_WithinHistoric_MovesTheAsset()
    {
        var request = MoveRequest(source: "Uncategorized", destination: "Closed 2024");
        request.AssetName = "CLOSEDASSET";
        request.Scope = "historic";

        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets/move", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var moved = await Client.GetAsync("/api/v1/financial/assets/XPI/Closed%202024/CLOSEDASSET?scope=historic");
        moved.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static MoveAssetRequestDTO MoveRequest(string source = "Default", string destination = "ISA") => new()
    {
        BrokerName = "XPI",
        Scope = "active",
        SourcePortfolioName = source,
        AssetName = "BCIA11",
        DestinationPortfolioName = destination
    };
}
