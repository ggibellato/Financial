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

    [Fact]
    public async Task GetBankBalancesByMonth_CombinesOpeningBalanceIncomeAndExpenses()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/api/v1/financial/banks/Barclays/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 100m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSource = "Gleison",
            GrossValue = null,
            NetValue = 500m,
            Bank = "Barclays"
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "Groceries",
            Value = 50m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });

        var response = await client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 550m);
    }

    [Fact]
    public async Task GetBankBalancesByMonth_WithNoActivity_ReturnsOpeningBalanceForEveryBank()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().HaveCount(3);
        balances.Should().OnlyContain(b => b.Balance == 0m);
    }

    [Fact]
    public async Task GetBankBalancesByMonth_ReflectsATransferForBothSourceAndDestinationBanks()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/api/v1/financial/banks/Barclays/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 1000m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        await client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            SourceBank = "Barclays",
            DestinationBank = "Trading212",
            Amount = 500m
        });

        var response = await client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 500m);
        balances.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == 500m);
    }

    [Fact]
    public async Task GetBankBalancesByMonth_ReflectsABalanceAdjustment()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/api/v1/financial/banks/Barclays/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 100m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        await client.PostAsJsonAsync("/api/v1/financial/banks/Barclays/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            TargetBalance = 150m
        });

        var response = await client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 150m);
    }
}
