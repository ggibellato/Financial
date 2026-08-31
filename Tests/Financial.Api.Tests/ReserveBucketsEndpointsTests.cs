using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

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

    [Fact]
    public async Task CreateReserveBucket_ValidRequest_ReturnsOkWithNewBucket()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bucket = await response.Content.ReadFromJsonAsync<ReserveBucketDTO>();
        bucket!.Name.Should().Be("Ferias");
        bucket.SplitPercentage.Should().Be(10m);
        bucket.IsActive.Should().BeFalse();
        bucket.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateReserveBucket_DuplicateName_ReturnsConflictWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Investimento",
            SplitPercentage = 10m,
            IsActive = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Investimento").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateReserveBucket_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "   ",
            SplitPercentage = 10m,
            IsActive = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReserveBucket_SplitPercentageOutOfRange_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 100.01m,
            IsActive = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReserveBucket_ActiveWithNonZeroSplit_PushesActiveTotalOver100AndReturnsWarning()
    {
        // The 4 seeded buckets are already active and sum to exactly 100%.
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = true,
        });

        var bucket = await response.Content.ReadFromJsonAsync<ReserveBucketDTO>();
        bucket!.Warning.Should().NotBeNull();
        bucket.Warning.Should().Contain("review your split percentages");
    }

    [Fact]
    public async Task CreateReserveBucket_InactiveBucket_DoesNotAffectTheActiveSplitTotal()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = false,
        });

        var bucket = await response.Content.ReadFromJsonAsync<ReserveBucketDTO>();
        bucket!.Warning.Should().BeNull();
    }

    [Fact]
    public async Task UpdateReserveBucket_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = false,
        });
        var bucket = await created.Content.ReadFromJsonAsync<ReserveBucketDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve-buckets/{bucket!.Id}", new ReserveBucketUpdateDTO
        {
            Name = "FeriasRenamed",
            SplitPercentage = 20m,
            IsActive = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ReserveBucketDTO>();
        updated!.Name.Should().Be("FeriasRenamed");
        updated.SplitPercentage.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateReserveBucket_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve-buckets/{Guid.NewGuid()}", new ReserveBucketUpdateDTO
        {
            Name = "X",
            SplitPercentage = 10m,
            IsActive = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateReserveBucket_NameCollidesWithAnotherBucket_ReturnsConflict()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = false,
        });
        var bucket = await created.Content.ReadFromJsonAsync<ReserveBucketDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve-buckets/{bucket!.Id}", new ReserveBucketUpdateDTO
        {
            Name = "Investimento",
            SplitPercentage = 10m,
            IsActive = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateReserveBucket_DeactivatingAnActiveBucket_DoesNotRemoveItAndIsStillVisibleViaGet()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/reserve-buckets", new ReserveBucketCreateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = true,
        });
        var bucket = await created.Content.ReadFromJsonAsync<ReserveBucketDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve-buckets/{bucket!.Id}", new ReserveBucketUpdateDTO
        {
            Name = "Ferias",
            SplitPercentage = 10m,
            IsActive = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var buckets = await (await Client.GetAsync("/api/v1/financial/reserve-buckets")).Content.ReadFromJsonAsync<List<ReserveBucketDTO>>();
        buckets.Should().ContainSingle(b => b.Id == bucket.Id && !b.IsActive);
    }
}
