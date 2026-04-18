using Financial.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Financial.Api.Tests;

public class NavigationEndpointsTests
{
    [Fact]
    public async Task GetNavigationTree_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/tree");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tree = await response.Content.ReadFromJsonAsync<TreeNodeDTO>();
        tree.Should().NotBeNull();
        tree!.NodeType.Should().NotBeNullOrWhiteSpace();
        tree.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetBrokers_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/navigation/brokers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brokers = await response.Content.ReadFromJsonAsync<BrokerNodeDTO[]>();
        brokers.Should().NotBeNull();
        brokers!.Length.Should().BeGreaterThan(0);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Repository:Provider"] = "LocalJson",
                        ["DataJsonFile"] = TestDataPaths.DataJsonFile
                    };
                    config.AddInMemoryCollection(settings);
                });
            });
    }
}
