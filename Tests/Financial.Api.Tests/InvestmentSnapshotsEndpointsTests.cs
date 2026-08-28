using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class InvestmentSnapshotsEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetSnapshotsForMonth_FirstCall_GeneratesElevenAccounts()
    {
        var response = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshots = await response.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        snapshots.Should().HaveCount(11);
        snapshots.Should().OnlyContain(s => s.Value == 0m);
        snapshots!.Count(s => s.IsLiability).Should().Be(6);
    }

    [Fact]
    public async Task GetSnapshotsForMonth_SecondCall_DoesNotDuplicateSnapshots()
    {
        await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");

        var response = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");

        var snapshots = await response.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        snapshots.Should().HaveCount(11);
    }

    [Fact]
    public async Task UpdateSnapshotValue_ExistingId_ReturnsOkAndUpdatesOnlyThatSnapshot()
    {
        var monthResponse = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var snapshots = await monthResponse.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var target = snapshots!.First(s => s.AccountName == "ChaseSave");

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{target.Id}", new InvestmentSnapshotValueUpdateDTO
        {
            Value = 500m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<InvestmentSnapshotDTO>();
        updated!.Value.Should().Be(500m);

        var refetch = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var refetched = await refetch.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        refetched!.Where(s => s.AccountName != "ChaseSave").Should().OnlyContain(s => s.Value == 0m);
    }

    [Fact]
    public async Task UpdateSnapshotValue_NegativeValue_ReturnsBadRequestWithMessage()
    {
        var monthResponse = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var snapshots = await monthResponse.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var target = snapshots!.First();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{target.Id}", new InvestmentSnapshotValueUpdateDTO
        {
            Value = -10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("negative");
    }

    [Fact]
    public async Task UpdateSnapshotValue_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{Guid.NewGuid()}", new InvestmentSnapshotValueUpdateDTO
        {
            Value = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
