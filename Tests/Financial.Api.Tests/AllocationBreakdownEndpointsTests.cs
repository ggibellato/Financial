using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests;

public class AllocationBreakdownEndpointsTests : ApiEndpointTests
{
    private const string AllocationBreakdownRoute = "/api/v1/financial/allocation-breakdown";

    private static readonly DateTimeOffset Today = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);

    public AllocationBreakdownEndpointsTests()
        : base(timeProvider: new FakeTimeProvider(Today))
    {
    }

    [Fact]
    public async Task GetAllocationBreakdown_Returns200WithEveryDimension()
    {
        await SetPriceAsync(130m);

        var response = await Client.GetAsync(AllocationBreakdownRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AllocationBreakdownDTO>();

        using var _ = new AssertionScope();
        dto.Should().NotBeNull();
        dto!.ByBroker.Should().ContainSingle(entry => entry.BrokerName == "XPI" && entry.Percentage == 100m);
        dto.ByCurrency.Should().ContainSingle(entry => entry.Currency == "BRL" && entry.Percentage == 100m);
        dto.ByClass.Should().ContainSingle();
        dto.ByCountry.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAllocationBreakdown_WhenNoActiveHoldingIsPriced_ReturnsFourEmptyDimensions()
    {
        var dto = await Client.GetFromJsonAsync<AllocationBreakdownDTO>(AllocationBreakdownRoute);

        using var _ = new AssertionScope();
        dto!.ByClass.Should().BeEmpty();
        dto.ByCurrency.Should().BeEmpty();
        dto.ByCountry.Should().BeEmpty();
        dto.ByBroker.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllocationBreakdown_RejectsAnUnknownSubRoute()
    {
        var response = await Client.GetAsync($"{AllocationBreakdownRoute}/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task SetPriceAsync(decimal price)
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = DateOnly.FromDateTime(Today.UtcDateTime),
            Price = price
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
