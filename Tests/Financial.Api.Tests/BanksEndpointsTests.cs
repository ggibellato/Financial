using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class BanksEndpointsTests
{
    [Fact]
    public async Task GetBanks_ReturnsTheThreeSeededBanksWithCorrectRoundUpFlags()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banks = await response.Content.ReadFromJsonAsync<List<BankDTO>>();
        banks.Should().HaveCount(3);
        banks.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
        banks.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
        banks.Should().ContainSingle(b => b.Name == "Chase" && b.RoundUpEnabled);
    }

    [Fact]
    public async Task UpdateOpeningBalance_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 1250.75m,
            OpeningBalanceDate = new DateOnly(2026, 7, 1)
        };

        var response = await client.PutAsJsonAsync("/api/v1/financial/banks/Barclays/opening-balance", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bank = await response.Content.ReadFromJsonAsync<BankDTO>();
        bank!.OpeningBalance.Should().Be(1250.75m);
        bank.OpeningBalanceDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task UpdateOpeningBalance_NegativeBalance_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = -1m,
            OpeningBalanceDate = new DateOnly(2026, 7, 1)
        };

        var response = await client.PutAsJsonAsync("/api/v1/financial/banks/Barclays/opening-balance", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateOpeningBalance_UnknownBankName_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 10m,
            OpeningBalanceDate = new DateOnly(2026, 7, 1)
        };

        var response = await client.PutAsJsonAsync("/api/v1/financial/banks/NotABank/opening-balance", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOpeningBalance_ThenGetBanks_ReflectsTheUpdate()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/api/v1/financial/banks/Chase/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 500m,
            OpeningBalanceDate = new DateOnly(2026, 6, 15)
        });

        var response = await client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banks = await response.Content.ReadFromJsonAsync<List<BankDTO>>();
        banks.Should().ContainSingle(b => b.Name == "Chase" && b.OpeningBalance == 500m && b.OpeningBalanceDate == new DateOnly(2026, 6, 15));
    }
}
