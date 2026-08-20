using System.Net;
using System.Net.Http.Json;
using Financial.Api.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Infrastructure.Sync;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.Api.Tests;

public class SyncStatusEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetSyncStatus_ReturnsOk_WithBothContextsIdle()
    {
        var response = await Client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<SyncStatusResponseDTO>();
        status!.CashFlow.State.Should().Be("Idle");
        status.CashFlow.LastError.Should().BeNull();
        status.CashFlow.LastSuccessfulSaveUtc.Should().BeNull();
        status.Investment.State.Should().Be("Idle");
        status.Investment.LastError.Should().BeNull();
        status.Investment.LastSuccessfulSaveUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetSyncStatus_JsonUsesCamelCasePropertyNames()
    {
        var response = await Client.GetAsync("/api/v1/financial/sync-status");
        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("\"cashFlow\"");
        json.Should().Contain("\"investment\"");
        json.Should().Contain("\"state\"");
        json.Should().Contain("\"lastError\"");
        json.Should().Contain("\"lastSuccessfulSaveUtc\"");
    }

    [Fact]
    public async Task GetSyncStatus_WhenCashFlowRepositoryIsFailed_ReflectsFailedStateForCashFlowOnly()
    {
        var failedTimestamp = new DateTime(2026, 8, 13, 9, 12, 4, DateTimeKind.Utc);
        var repository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "Drive request failed with a transient status (503 ServiceUnavailable).", failedTimestamp)
        };
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICashFlowRepository>();
                services.AddSingleton<ICashFlowRepository>(repository);
            }));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<SyncStatusResponseDTO>();
        status!.CashFlow.State.Should().Be("Failed");
        status.CashFlow.LastError.Should().Be("Drive request failed with a transient status (503 ServiceUnavailable).");
        status.CashFlow.LastSuccessfulSaveUtc.Should().Be(failedTimestamp);
        status.Investment.State.Should().Be("Idle");
    }
}
