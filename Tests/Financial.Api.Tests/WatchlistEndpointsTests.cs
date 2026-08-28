using Financial.Investment.Application.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class WatchlistEndpointsTests
{
    private static readonly JsonSerializerOptions CaseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Boots the API with the WatchlistOptions a test needs; the configuration callback is the only
    /// thing that differs between these tests.</summary>
    private static WebApplicationFactory<Program> CreateFactory(Action<WatchlistOptions> configure) =>
        new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.PostConfigure(configure)));

    [Fact]
    public async Task GetWatchlist_ReturnsOk_WithConfiguredItems()
    {
        await using var factory = CreateFactory(options =>
        {
            options.Items =
            [
                new WatchlistItemDTO { Group = "Group A", Name = "KLBN4" },
                new WatchlistItemDTO { Group = "Group A", Name = "TAEE3" },
            ];
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<WatchlistItemDTO[]>(CaseInsensitiveJson);
        items.Should().HaveCount(2);
        items![0].Group.Should().Be("Group A");
        items[0].Name.Should().Be("KLBN4");
        items[1].Name.Should().Be("TAEE3");
    }

    [Fact]
    public async Task GetWatchlist_ReturnsEmptyArray_WhenNoItemsConfigured()
    {
        await using var factory = CreateFactory(options => options.Items.Clear());

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<WatchlistItemDTO[]>(CaseInsensitiveJson);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWatchlist_JsonUsesGroupAndNameProperties()
    {
        await using var factory = CreateFactory(options =>
        {
            options.Items = [new WatchlistItemDTO { Group = "Test", Name = "KLBN4" }];
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
