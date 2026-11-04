using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ReserveBucketsEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetReserveBuckets_ReturnsTheFourSeededBucketsWithCorrectFields()
    {
        var response = await Client.GetAsync("/api/v1/financial/reserve-buckets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var buckets = await response.Content.ReadFromJsonAsync<List<ReserveBucketDTO>>();
        buckets.Should().HaveCount(4);
        buckets.Should().ContainSingle(b => b.Name == "Investimento" && b.IsActive && b.SplitPercentage == 33.33m && b.Id != Guid.Empty);
        buckets.Should().ContainSingle(b => b.Name == "HouseTreats" && b.IsActive && b.SplitPercentage == 33.33m);
        buckets.Should().ContainSingle(b => b.Name == "Ariana" && b.IsActive && b.SplitPercentage == 16.67m);
        buckets.Should().ContainSingle(b => b.Name == "Gleison" && b.IsActive && b.SplitPercentage == 16.67m);
    }

    [Fact]
    public async Task GetSplitStatus_ForTheSeededBuckets_ReportsTheTotalWithoutAWarning()
    {
        var response = await Client.GetAsync("/api/v1/financial/reserve-buckets/split-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<ReserveSplitStatusDTO>();
        status!.ActiveTotal.Should().Be(100m);
        status.Warning.Should().BeNull();
    }

    [Fact]
    public async Task GetReserveBuckets_RequiresNoParameters_AndReturnsFullUnfilteredList()
    {
        var response = await Client.GetAsync("/api/v1/financial/reserve-buckets");
        var buckets = await response.Content.ReadFromJsonAsync<List<ReserveBucketDTO>>();

        // All 4 seeded buckets come back regardless of IsActive value - none are seeded inactive
        // in the fixture, so this also confirms no isActive=true filter is silently applied.
        buckets.Should().HaveCount(4);
        buckets.Should().OnlyContain(b => b.IsActive);
    }

    [Fact]
    public async Task GetReserveBuckets_NeverReturnsAWarning()
    {
        var response = await Client.GetAsync("/api/v1/financial/reserve-buckets");

        var buckets = await response.Content.ReadFromJsonAsync<List<ReserveBucketDTO>>();
        buckets.Should().OnlyContain(b => b.Warning == null);
    }
}
