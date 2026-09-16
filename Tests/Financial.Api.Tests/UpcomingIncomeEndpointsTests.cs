using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests;

public class UpcomingIncomeEndpointsTests : ApiEndpointTests
{
    private const string UpcomingIncomeRoute = "/api/v1/financial/upcoming-income";

    [Fact]
    public async Task GetUpcomingIncome_Returns200WithOneEntryPerProjectableHolding()
    {
        var response = await Client.GetAsync(UpcomingIncomeRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = await response.Content.ReadFromJsonAsync<List<UpcomingIncomeDTO>>();

        using var _ = new AssertionScope();
        entries.Should().NotBeNull();
        var entry = entries!.Should().ContainSingle().Subject;
        entry.AssetName.Should().Be("BCIA11");
        entry.BrokerName.Should().Be("XPI");
        entry.LastCreditDate.Should().Be(new DateTime(2024, 3, 1));
        entry.ProjectedNextDate.Should().Be(new DateTime(2024, 4, 1));
        entry.ProjectedAmount.Should().Be(6m);
    }

    [Fact]
    public async Task GetUpcomingIncome_RejectsAnUnknownSubRoute()
    {
        var response = await Client.GetAsync($"{UpcomingIncomeRoute}/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
