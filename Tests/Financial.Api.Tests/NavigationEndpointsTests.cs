using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class NavigationEndpointsTests
{
    [Fact]
    public async Task GetNavigationTree_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/tree");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();
        tree.Should().NotBeNull();
        tree!.NodeType.Should().Be(TreeNodeType.Investments);
        tree.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetBrokers_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/brokers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brokers = await response.Content.ReadFromJsonAsync<BrokerNodeDTO[]>();
        brokers.Should().NotBeNull();
        brokers!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetNavigationTree_ScopeOmitted_PreservesActiveOnlyBehavior()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/tree");

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();

        var assetNames = GetAllAssetNodes(tree!).Select(a => a.DisplayName).ToList();
        assetNames.Should().Contain("BCIA11");
        assetNames.Should().NotContain("CLOSEDASSET");
    }

    [Fact]
    public async Task GetNavigationTree_ScopeActive_AssetNode_IncludesPositionType()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/tree?scope=active");

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();

        var assetNode = GetAllAssetNodes(tree!).Single(a => a.DisplayName == "BCIA11");
        assetNode.Metadata.Should().ContainKey("PositionType");
        ((System.Text.Json.JsonElement)assetNode.Metadata["PositionType"]).GetString().Should().Be("Long");
    }

    [Fact]
    public async Task GetNavigationTree_ScopeHistoric_ReturnsOnlyHistoricBroker()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/tree?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();
        var assetNames = GetAllAssetNodes(tree!).Select(a => a.DisplayName).ToList();

        assetNames.Should().Contain("CLOSEDASSET");
        assetNames.Should().NotContain("BCIA11");
    }

    [Fact]
    public async Task GetBrokers_ScopeHistoric_ReturnsOnlyHistoricBroker()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/brokers?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brokers = await response.Content.ReadFromJsonAsync<BrokerNodeDTO[]>();
        brokers.Should().NotBeNull();
        brokers!.Should().ContainSingle(b => b.Portfolios.Any(p => p.Assets.Any(a => a.Name == "CLOSEDASSET")));
    }

    private static IEnumerable<TreeNodeDTO> GetAllAssetNodes(TreeNodeDTO tree) =>
        tree.Children.SelectMany(broker => broker.Children).SelectMany(portfolio => portfolio.Children);
}
