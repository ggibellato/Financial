using Financial.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class XirrEndpointsTests
{
    [Fact]
    public async Task Calculate_ValidRequest_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var request = new CalculateXirrRequestDTO
        {
            CashFlows = [new AssetCashFlowDTO { Date = DateTime.Today.AddYears(-1), Amount = -1000m }],
            TerminalValue = 1100m
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/xirr/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<XirrResultDTO>();
        result.Should().NotBeNull();
        result!.Xirr.Should().NotBeNull();
        result.Xirr!.Value.Should().BeApproximately(0.10m, 0.01m);
    }

    [Fact]
    public async Task Calculate_EmptyBody_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync<CalculateXirrRequestDTO?>("/api/v1/financial/xirr/calculate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Calculate_InsufficientCashFlows_ReturnsOkWithNullXirr()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var request = new CalculateXirrRequestDTO { CashFlows = [], TerminalValue = 0m };

        var response = await client.PostAsJsonAsync("/api/v1/financial/xirr/calculate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<XirrResultDTO>();
        result.Should().NotBeNull();
        result!.Xirr.Should().BeNull();
    }
}
