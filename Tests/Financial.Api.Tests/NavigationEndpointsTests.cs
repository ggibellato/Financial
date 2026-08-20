using Financial.Investment.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class NavigationEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetNavigationTree_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/navigation/tree");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();
        tree.Should().NotBeNull();
        tree!.NodeType.Should().Be(TreeNodeType.Investments);
        tree.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetBrokers_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/navigation/brokers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brokers = await response.Content.ReadFromJsonAsync<BrokerNodeDTO[]>();
        brokers.Should().NotBeNull();
        brokers!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetNavigationTree_ScopeOmitted_PreservesActiveOnlyBehavior()
    {
        var response = await Client.GetAsync("/api/v1/financial/navigation/tree");

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();

        var assetNames = GetAllAssetNodes(tree!).Select(a => a.DisplayName).ToList();
        assetNames.Should().Contain("BCIA11");
        assetNames.Should().NotContain("CLOSEDASSET");
    }

    [Fact]
    public async Task GetNavigationTree_ScopeActive_AssetNode_IncludesPositionType()
    {
        var response = await Client.GetAsync("/api/v1/financial/navigation/tree?scope=active");

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();

        var assetNode = GetAllAssetNodes(tree!).Single(a => a.DisplayName == "BCIA11");
        assetNode.Metadata.Should().ContainKey("PositionType");
        ((System.Text.Json.JsonElement)assetNode.Metadata["PositionType"]).GetString().Should().Be("Long");
    }

    [Fact]
    public async Task GetNavigationTree_ScopeHistoric_ReturnsOnlyHistoricBroker()
    {
        var response = await Client.GetAsync("/api/v1/financial/navigation/tree?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();
        var assetNames = GetAllAssetNodes(tree!).Select(a => a.DisplayName).ToList();

        assetNames.Should().Contain("CLOSEDASSET");
        assetNames.Should().NotContain("BCIA11");
    }

    [Fact]
    public async Task GetBrokers_ScopeHistoric_ReturnsOnlyHistoricBroker()
    {
        var response = await Client.GetAsync("/api/v1/financial/navigation/brokers?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brokers = await response.Content.ReadFromJsonAsync<BrokerNodeDTO[]>();
        brokers.Should().NotBeNull();
        brokers!.Should().ContainSingle(b => b.Portfolios.Any(p => p.Assets.Any(a => a.Name == "CLOSEDASSET")));
    }

    private static IEnumerable<TreeNodeDTO> GetAllAssetNodes(TreeNodeDTO tree) =>
        tree.Children.SelectMany(broker => broker.Children).SelectMany(portfolio => portfolio.Children);
}
