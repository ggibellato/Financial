using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ReserveEndpointsTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private static readonly Guid ArianaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000003");
    private static readonly Guid ArianaIncomeSourceId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000002");
    private static readonly Guid ReservaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-900000000001");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");

    private async Task<List<AccountTransactionDTO>> ReservaRowsAsync() =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{ReservaId}/transactions/2026/7"))!;

    [Fact]
    public async Task UpdateMovement_WithdrawalPostedThroughThePotsRoute_Returns409()
    {
        var withdrawal = await Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", new PotWithdrawalRequestDTO
        {
            PotId = ArianaId,
            Amount = 30m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Groceries",
            Confirmed = true
        });
        var operation = await withdrawal.Content.ReadFromJsonAsync<PotOperationDTO>();
        var movementId = (await ReservaRowsAsync()).Single(row => row.Operation?.OperationId == operation!.OperationId).Id;

        var response = await Client.PutAsJsonAsync($"{Api}/reserve/movements/{movementId}", new ReserveMovementUpdateDTO
        {
            BucketId = ArianaId,
            Amount = -45m,
            Date = new DateOnly(2026, 7, 3),
            Description = "Groceries (corrected)"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateMovement_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"{Api}/reserve/movements/{Guid.NewGuid()}", new ReserveMovementUpdateDTO
        {
            BucketId = ArianaId,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMovement_LineOfADepositPostedThroughThePotsRoute_Returns409AndKeepsAllFourLines()
    {
        await Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/deposits", new PotDepositRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });
        var oneLineOfTheDeposit = (await ReservaRowsAsync()).First();

        var response = await Client.DeleteAsync($"{Api}/reserve/movements/{oneLineOfTheDeposit.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReservaRowsAsync()).Should().HaveCount(4);
    }

    [Fact]
    public async Task DeleteMovement_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"{Api}/reserve/movements/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateLinkedMovementIdAsync()
    {
        var income = await Client.PostAsJsonAsync($"{Api}/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = ArianaIncomeSourceId,
            GrossValue = null,
            NetValue = 2450.00m,
            BankId = ChaseId,
            PotsAccountId = ReservaId
        });
        var incomeDto = await income.Content.ReadFromJsonAsync<IncomeDTO>();
        return (await ReservaRowsAsync()).First(row => row.Movement?.IncomeId == incomeDto!.Id).Id;
    }

    [Fact]
    public async Task UpdateMovement_OnLinkedMovement_ReturnsConflict()
    {
        var linkedMovementId = await CreateLinkedMovementIdAsync();

        var response = await Client.PutAsJsonAsync($"{Api}/reserve/movements/{linkedMovementId}", new ReserveMovementUpdateDTO
        {
            BucketId = ArianaId,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Attempted direct edit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("linked to an income");
    }

    [Fact]
    public async Task DeleteMovement_OnLinkedMovement_ReturnsConflict()
    {
        var linkedMovementId = await CreateLinkedMovementIdAsync();

        var response = await Client.DeleteAsync($"{Api}/reserve/movements/{linkedMovementId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("linked to an income");
    }
}
