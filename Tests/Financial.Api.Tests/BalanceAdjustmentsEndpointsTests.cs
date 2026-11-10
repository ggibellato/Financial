using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class BalanceAdjustmentsEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid CreditCardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001");

    [Fact]
    public async Task AddAdjustment_ValidRequest_ReturnsCreatedWithComputedDelta()
    {
        var request = new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150.00m,
            Note = "Matched against July statement"
        };

        var response = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var adjustment = await response.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();
        adjustment.Should().NotBeNull();
        adjustment!.BankId.Should().Be(BarclaysId);
        adjustment.BankName.Should().Be("Barclays");
        adjustment.TargetBalance.Should().Be(150.00m);
        adjustment.Delta.Should().Be(150.00m);
        adjustment.Note.Should().Be("Matched against July statement");
    }

    [Fact]
    public async Task AddAdjustment_UnknownAccount_ReturnsNotFound()
    {
        var request = new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        };

        var response = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{Guid.NewGuid()}/adjustments", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Account").And.Contain("was not found");
    }

    [Fact]
    public async Task AddAdjustment_NegativeTargetBalance_ReturnsBadRequest()
    {
        var request = new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = -1m
        };

        var response = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAdjustment_ExistingId_ReturnsOkAndRecomputesDelta()
    {
        var created = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        var createdAdjustment = await created.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments/{createdAdjustment!.Id}", new BalanceAdjustmentUpdateDTO
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
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments/{Guid.NewGuid()}", new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAdjustment_UnknownAccountId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/{Guid.NewGuid()}/adjustments/{Guid.NewGuid()}", new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAdjustment_ExistingId_ReturnsNoContentAndRemovesAdjustment()
    {
        var created = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        var createdAdjustment = await created.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments/{createdAdjustment!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await AdjustmentIdsAsync(BarclaysId)).Should().NotContain(createdAdjustment.Id);
    }

    [Fact]
    public async Task DeleteAdjustment_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAdjustment_UnknownAccountId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/accounts/{Guid.NewGuid()}/adjustments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddAdjustment_AccountThatCannotBeReconciled_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{CreditCardId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("cannot be reconciled");
    }

    [Fact]
    public async Task AddAdjustment_NullBody_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(
            $"/api/v1/financial/accounts/{BarclaysId}/adjustments",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAdjustment_NullBody_ReturnsBadRequest()
    {
        var response = await Client.PutAsync(
            $"/api/v1/financial/accounts/{BarclaysId}/adjustments/{Guid.NewGuid()}",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAdjustment_AdjustmentOfAnotherAccount_ReturnsNotFound()
    {
        var created = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        var createdAdjustment = await created.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/{ChaseId}/adjustments/{createdAdjustment!.Id}", new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 80m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Balance adjustment");
    }

    [Fact]
    public async Task DeleteAdjustment_AdjustmentOfAnotherAccount_ReturnsNotFoundAndKeepsIt()
    {
        var created = await Client.PostAsJsonAsync($"/api/v1/financial/accounts/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 100m
        });
        var createdAdjustment = await created.Content.ReadFromJsonAsync<BalanceAdjustmentDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/accounts/{ChaseId}/adjustments/{createdAdjustment!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AdjustmentIdsAsync(BarclaysId)).Should().Contain(createdAdjustment.Id);
    }

    [Theory]
    [InlineData("POST", "")]
    [InlineData("PUT", "/8f3b1c1a-2e3a-4b1a-9a7f-900000000001")]
    [InlineData("DELETE", "/8f3b1c1a-2e3a-4b1a-9a7f-900000000001")]
    public async Task BankScopedAdjustmentWrites_AreGone(string method, string suffix)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/financial/banks/{BarclaysId}/adjustments{suffix}");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    private async Task<List<Guid>> AdjustmentIdsAsync(Guid accountId) =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"/api/v1/financial/accounts/{accountId}/transactions/2026/7"))!
            .Where(row => row.Adjustment is not null)
            .Select(row => row.Adjustment!.Id)
            .ToList();
}
