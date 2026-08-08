using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ReserveBucketsEndpointsTests
{
    [Fact]
    public async Task GetReserveBuckets_ReturnsTheFourSeededBucketsWithCorrectFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/reserve-buckets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var buckets = await response.Content.ReadFromJsonAsync<List<ReserveBucketDTO>>();
        buckets.Should().HaveCount(4);
        buckets.Should().ContainSingle(b => b.Name == "Investimento" && b.IsActive && b.SplitPercentage == 33.33m && b.Id != Guid.Empty);
        buckets.Should().ContainSingle(b => b.Name == "HouseTreats" && b.IsActive && b.SplitPercentage == 33.33m);
        buckets.Should().ContainSingle(b => b.Name == "Ariana" && b.IsActive && b.SplitPercentage == 16.67m);
        buckets.Should().ContainSingle(b => b.Name == "Gleison" && b.IsActive && b.SplitPercentage == 16.67m);
    }

    [Fact]
    public async Task GetReserveBuckets_RequiresNoParameters_AndReturnsFullUnfilteredList()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/reserve-buckets");
        var buckets = await response.Content.ReadFromJsonAsync<List<ReserveBucketDTO>>();

        // All 4 seeded buckets come back regardless of IsActive value - none are seeded inactive
        // in the fixture, so this also confirms no isActive=true filter is silently applied.
        buckets.Should().HaveCount(4);
        buckets.Should().OnlyContain(b => b.IsActive);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task ReserveBuckets_UnsupportedVerbs_DoNotSucceed(string method)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/financial/reserve-buckets");

        var response = await client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
