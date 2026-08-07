using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class BalanceAdjustmentsEndpointsTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");

    [Fact]
    public async Task AddAdjustment_ValidRequest_ReturnsOkWithComputedDelta()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150.00m,
            Note = "Matched against July statement"
        };

        var response = await client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var adjustment = await response.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();
        adjustment.Should().NotBeNull();
        adjustment!.BankId.Should().Be(BarclaysId);
        adjustment.BankName.Should().Be("Barclays");
        adjustment.TargetBalance.Should().Be(150.00m);
        adjustment.Delta.Should().Be(150.00m);
        adjustment.Note.Should().Be("Matched against July statement");
    }

    [Fact]
    public async Task AddAdjustment_UnresolvableBank_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        };

        var response = await client.PostAsJsonAsync($"/api/v1/financial/banks/{Guid.NewGuid()}/adjustments", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddAdjustment_NegativeTargetBalance_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = -1m
        };

        var response = await client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAdjustment_ExistingId_ReturnsOkAndRecomputesDelta()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        var createdAdjustment = await created.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments/{createdAdjustment!.Id}", new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 80m,
            Note = "Corrected"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();
        updated!.TargetBalance.Should().Be(80m);
        updated.Delta.Should().Be(80m);
        updated.Note.Should().Be("Corrected");
    }

    [Fact]
    public async Task UpdateAdjustment_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments/{Guid.NewGuid()}", new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAdjustment_UnknownBankId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/banks/{Guid.NewGuid()}/adjustments/{Guid.NewGuid()}", new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAdjustment_ExistingId_ReturnsOkAndRemovesAdjustment()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        var createdAdjustment = await created.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();

        var response = await client.DeleteAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments/{createdAdjustment!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetFromJsonAsync<List<BalanceAdjustmentDTO>>($"/api/v1/financial/banks/{BarclaysId}/adjustments");
        list.Should().NotContain(a => a.Id == createdAdjustment.Id);
    }

    [Fact]
    public async Task DeleteAdjustment_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAdjustment_UnknownBankId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/financial/banks/{Guid.NewGuid()}/adjustments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAdjustmentsByBank_ReturnsOnlyThatBanksAdjustments()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        await client.PostAsJsonAsync($"/api/v1/financial/banks/{ChaseId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 50m
        });

        var response = await client.GetAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<BalanceAdjustmentDTO>>();
        items.Should().ContainSingle();
        items!.Single().BankId.Should().Be(BarclaysId);
        items.Single().BankName.Should().Be("Barclays");
    }

    [Fact]
    public async Task GetAdjustmentsByBank_UnknownBankId_ReturnsEmptyList()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/financial/banks/{Guid.NewGuid()}/adjustments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<BalanceAdjustmentDTO>>();
        items.Should().BeEmpty();
    }
}
