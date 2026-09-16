using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests;

public class DashboardEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetDashboard_Returns200WithTheAggregate()
    {
        var response = await Client.GetAsync("/api/v1/financial/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<PortfolioDashboardDTO>();

        using var _ = new AssertionScope();
        dto.Should().NotBeNull();
        dto!.Invested.Should().BeGreaterThan(0m);
        dto.UnvaluedHoldingCount.Should().BeGreaterThanOrEqualTo(0);
        dto.ReportingCurrency.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetDashboard_RejectsAnUnknownSubRoute()
    {
        var response = await Client.GetAsync("/api/v1/financial/dashboard/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
