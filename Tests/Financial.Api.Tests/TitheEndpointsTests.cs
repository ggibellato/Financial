using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class TitheEndpointsTests
{
    [Fact]
    public async Task GetTitheSummaryByMonth_WithIncomeAndDizimoExpense_ReturnsCalculatedFigures()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSource = "Gleison",
            GrossValue = null,
            NetValue = 3000m,
            Bank = "Barclays"
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Tithe payment",
            Value = 200m,
            Category = "Dizimo",
            PaymentSource = "Barclays",
            CardTag = null
        });

        var response = await client.GetAsync("/api/v1/financial/tithe/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CalculatedTithe.Should().Be(300m);
        summary.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryByMonth_WithNoData_ReturnsZeros()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/tithe/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CalculatedTithe.Should().Be(0m);
        summary.TitheBalance.Should().Be(0m);
    }
}
