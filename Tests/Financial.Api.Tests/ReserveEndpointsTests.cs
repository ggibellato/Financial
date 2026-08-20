using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ReserveEndpointsTests : ApiEndpointTests
{
    private static readonly Guid InvestimentoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000001");
    private static readonly Guid ArianaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000003");

    [Fact]
    public async Task PostIncomeSplit_ValidRequest_ReturnsOkWithComputedSplit()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeSplitResultDTO>();
        result.Should().NotBeNull();
        result!.Buckets.Should().ContainSingle(b => b.BucketName == "Investimento" && b.Amount == 654.27m);
        result.Total.Should().Be(1963.00m);
    }

    [Fact]
    public async Task PostIncomeSplit_NonPositiveAmount_ReturnsBadRequestWithMessage()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 0m,
            Description = "Ramsay"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task PostIncomeSplit_MissingDescription_ReturnsBadRequestWithMessage()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = ""
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Description is required");
    }

    [Fact]
    public async Task GetBucketBalances_ReflectsPostedIncomeSplit()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.GetAsync("/api/v1/financial/reserve/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<ReserveBucketBalanceDTO>>();
        balances.Should().HaveCount(4);
        balances.Should().ContainSingle(b => b.BucketName == "Investimento" && b.Balance == 654.27m);
        balances.Should().ContainSingle(b => b.BucketId == InvestimentoId && b.Balance == 654.27m);
    }

    [Fact]
    public async Task PostWithdrawal_ExceedingBalanceUnconfirmed_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = ArianaId,
            Amount = 99999m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Too much",
            Confirmed = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Ariana");
    }

    [Fact]
    public async Task PostWithdrawal_ExceedingBalanceConfirmed_ReturnsOk()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = ArianaId,
            Amount = 99999m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Too much but confirmed",
            Confirmed = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movement = await response.Content.ReadFromJsonAsync<ReserveMovementDTO>();
        movement!.Amount.Should().Be(-99999m);
    }

    [Fact]
    public async Task PostWithdrawal_WithinBalance_UpdatesOnlyThatBucket()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });
        var balancesBefore = await Client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>("/api/v1/financial/reserve/balances");
        var gleisonBefore = balancesBefore!.Single(b => b.BucketName == "Gleison").Balance;

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = InvestimentoId,
            Amount = 100m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Small withdrawal",
            Confirmed = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balancesAfter = await Client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>("/api/v1/financial/reserve/balances");
        balancesAfter!.Single(b => b.BucketName == "Gleison").Balance.Should().Be(gleisonBefore);
    }

    [Fact]
    public async Task GetMovementHistory_ReturnsAllPostedMovements()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.GetAsync("/api/v1/financial/reserve/movements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movements = await response.Content.ReadFromJsonAsync<List<ReserveMovementDTO>>();
        movements.Should().HaveCount(4);
        movements.Should().OnlyContain(m => m.Description == "Ramsay");
    }

    [Fact]
    public async Task UpdateMovement_ExistingId_ReturnsOkAndUpdatesFields()
    {
        var withdrawal = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = ArianaId,
            Amount = 30m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Groceries",
            Confirmed = true
        });
        var movement = await withdrawal.Content.ReadFromJsonAsync<ReserveMovementDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve/movements/{movement!.Id}", new UpdateReserveMovementDTO
        {
            BucketId = ArianaId,
            Amount = -45m,
            Date = new DateOnly(2026, 7, 3),
            Description = "Groceries (corrected)"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ReserveMovementDTO>();
        updated!.Amount.Should().Be(-45m);
        updated.Description.Should().Be("Groceries (corrected)");
    }

    [Fact]
    public async Task UpdateMovement_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve/movements/{Guid.NewGuid()}", new UpdateReserveMovementDTO
        {
            BucketId = ArianaId,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMovement_MovementFromASplit_DeletesAllFourLines()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        var oneLineOfTheSplit = movements!.First();

        var response = await Client.DeleteAsync($"/api/v1/financial/reserve/movements/{oneLineOfTheSplit.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var remaining = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMovement_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/reserve/movements/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
