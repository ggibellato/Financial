using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ReserveEndpointsTests
{
    [Fact]
    public async Task PostIncomeSplit_ValidRequest_ReturnsOkWithComputedSplit()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeSplitResultDTO>();
        result.Should().NotBeNull();
        result!.Investimento.Should().Be(654.33m);
        result.Total.Should().Be(1963.00m);
    }

    [Fact]
    public async Task PostIncomeSplit_NonPositiveAmount_ReturnsBadRequestWithMessage()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 0m
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task GetBucketBalances_ReflectsPostedIncomeSplit()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m
        });

        var response = await client.GetAsync("/api/v1/financial/reserve/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<ReserveBucketBalanceDTO>>();
        balances.Should().HaveCount(4);
        balances.Should().ContainSingle(b => b.Bucket == "Investimento" && b.Balance == 654.33m);
    }

    [Fact]
    public async Task PostWithdrawal_ExceedingBalanceUnconfirmed_ReturnsConflictWithMessage()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m
        });

        var response = await client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            Bucket = "Ariana",
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
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m
        });

        var response = await client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            Bucket = "Ariana",
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
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m
        });
        var balancesBefore = await client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>("/api/v1/financial/reserve/balances");
        var gleisonBefore = balancesBefore!.Single(b => b.Bucket == "Gleison").Balance;

        var response = await client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            Bucket = "Investimento",
            Amount = 100m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Small withdrawal",
            Confirmed = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balancesAfter = await client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>("/api/v1/financial/reserve/balances");
        balancesAfter!.Single(b => b.Bucket == "Gleison").Balance.Should().Be(gleisonBefore);
    }

    [Fact]
    public async Task GetMovementHistory_ReturnsAllPostedMovements()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m
        });

        var response = await client.GetAsync("/api/v1/financial/reserve/movements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movements = await response.Content.ReadFromJsonAsync<List<ReserveMovementDTO>>();
        movements.Should().HaveCount(4);
    }
}
