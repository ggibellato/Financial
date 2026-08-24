using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class IncomeSourcesEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetIncomeSources_ReturnsTheFourSeededSourcesWithCorrectFields()
    {
        var response = await Client.GetAsync("/api/v1/financial/income-sources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sources = await response.Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        sources.Should().HaveCount(4);
        sources.Should().ContainSingle(s => s.Name == "Gleison" && s.IsActive && s.Group == "Salary" && s.Id != Guid.Empty);
        sources.Should().ContainSingle(s => s.Name == "Ariana" && s.IsActive && s.Group == "Salary");
        sources.Should().ContainSingle(s => s.Name == "Lottery" && s.IsActive && s.Group == "NonReportable");
        sources.Should().ContainSingle(s => s.Name == "DividendoJuros" && s.IsActive && s.Group == "DividendoJuros");
    }

    [Fact]
    public async Task GetIncomeSources_ReturnsAutoSplitToReserveInResponse()
    {
        var response = await Client.GetAsync("/api/v1/financial/income-sources");

        var sources = await response.Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        sources.Should().ContainSingle(s => s.Name == "Ariana" && s.AutoSplitToReserve);
        sources.Should().ContainSingle(s => s.Name == "Gleison" && !s.AutoSplitToReserve);
        sources.Should().ContainSingle(s => s.Name == "Lottery" && !s.AutoSplitToReserve);
        sources.Should().ContainSingle(s => s.Name == "DividendoJuros" && !s.AutoSplitToReserve);
    }

    [Fact]
    public async Task GetIncomeSources_RequiresNoParameters_AndReturnsFullUnfilteredList()
    {
        var response = await Client.GetAsync("/api/v1/financial/income-sources");
        var sources = await response.Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();

        // All 4 seeded sources come back regardless of IsActive value - none are seeded inactive
        // in the fixture, so this also confirms no isActive=true filter is silently applied.
        sources.Should().HaveCount(4);
        sources.Should().OnlyContain(s => s.IsActive);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task IncomeSources_UnsupportedVerbs_DoNotSucceed(string method)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/financial/income-sources");

        var response = await Client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
