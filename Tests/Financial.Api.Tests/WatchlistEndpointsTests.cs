using Financial.Investment.Application.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class WatchlistEndpointsTests
{
    [Fact]
    public async Task GetWatchlist_ReturnsOk_WithConfiguredItems()
    {
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<WatchlistOptions>(options =>
                {
                    options.Items =
                    [
                        new WatchlistItem { Group = "Group A", Name = "KLBN4" },
                        new WatchlistItem { Group = "Group A", Name = "TAEE3" },
                    ];
                });
            });
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<WatchlistItem[]>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        items.Should().HaveCount(2);
        items![0].Group.Should().Be("Group A");
        items[0].Name.Should().Be("KLBN4");
        items[1].Name.Should().Be("TAEE3");
    }

    [Fact]
    public async Task GetWatchlist_ReturnsEmptyArray_WhenNoItemsConfigured()
    {
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<WatchlistOptions>(options => options.Items.Clear());
            });
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<WatchlistItem[]>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWatchlist_JsonUsesGroupAndNameProperties()
    {
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<WatchlistOptions>(options =>
                {
                    options.Items = [new WatchlistItem { Group = "Test", Name = "KLBN4" }];
                });
            });
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/watchlist");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement[0];
        first.TryGetProperty("group", out _).Should().BeTrue("frontend expects camelCase 'group'");
        first.TryGetProperty("name", out _).Should().BeTrue("frontend expects camelCase 'name'");
    }
}
