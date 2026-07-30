using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class IncomesEndpointsTests
{
    [Fact]
    public async Task AddIncome_ValidRequest_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSource = "Gleison",
            GrossValue = 3200.00m,
            NetValue = 2450.00m,
            Bank = "Barclays"
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        income.Should().NotBeNull();
        income!.IncomeSource.Should().Be("Gleison");
        income.GrossValue.Should().Be(3200.00m);
        income.NetValue.Should().Be(2450.00m);
        income.Bank.Should().Be("Barclays");
    }

    [Fact]
    public async Task AddIncome_WithoutBank_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSource = "Lottery",
            GrossValue = null,
            NetValue = 50m,
            Bank = ""
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddIncome_NegativeNetValue_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSource = "Lottery",
            GrossValue = null,
            NetValue = -1m,
            Bank = "Chase"
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/incomes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateIncome_ExistingId_ReturnsOkAndUpdatesFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSource = "Lottery",
            GrossValue = null,
            NetValue = 10m,
            Bank = "Chase"
        });
        var createdIncome = await created.Content.ReadFromJsonAsync<IncomeDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/incomes/{createdIncome!.Id}", new IncomeUpdateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            IncomeSource = "DividendoJuros",
            GrossValue = null,
            NetValue = 25m,
            Bank = "Trading212"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<IncomeDTO>();
        updated!.IncomeSource.Should().Be("DividendoJuros");
        updated.NetValue.Should().Be(25m);
        updated.Bank.Should().Be("Trading212");
    }

    [Fact]
    public async Task UpdateIncome_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/incomes/{Guid.NewGuid()}", new IncomeUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSource = "Lottery",
            GrossValue = null,
            NetValue = 10m,
            Bank = "Chase"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteIncome_ExistingId_ReturnsOkAndRemovesIncome()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            IncomeSource = "Lottery",
            GrossValue = null,
            NetValue = 10m,
            Bank = "Chase"
        });
        var createdIncome = await created.Content.ReadFromJsonAsync<IncomeDTO>();

        var response = await client.DeleteAsync($"/api/v1/financial/incomes/{createdIncome!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetFromJsonAsync<List<IncomeDTO>>("/api/v1/financial/incomes/month/2026/7");
        list.Should().NotContain(i => i.Id == createdIncome.Id);
    }

    [Fact]
    public async Task DeleteIncome_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/financial/incomes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIncomesByMonth_ReturnsOnlyIncomesForThatMonthAndAllowsMultiplePerSource()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSource = "Ariana",
            GrossValue = null,
            NetValue = 400m,
            Bank = "Chase"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 8),
            IncomeSource = "Ariana",
            GrossValue = null,
            NetValue = 420m,
            Bank = "Chase"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            IncomeSource = "Ariana",
            GrossValue = null,
            NetValue = 410m,
            Bank = "Chase"
        });

        var response = await client.GetAsync("/api/v1/financial/incomes/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<IncomeDTO>>();
        items.Should().HaveCount(2);
        items.Should().OnlyContain(i => i.Date.Month == 7);
    }
}
