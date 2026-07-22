using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class YearlySummaryEndpointsTests
{
    [Fact]
    public async Task GetExpenseCategoryTotals_ReturnsAllCategoriesWithCorrectYearlyTotal()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 1, 5),
            Description = "January groceries",
            Value = 100m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 3, 5),
            Description = "March groceries",
            Value = 50m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });

        var response = await client.GetAsync("/api/v1/financial/yearly-summary/2026/expense-categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var totals = await response.Content.ReadFromJsonAsync<List<CategoryYearlyTotalDTO>>();
        totals.Should().HaveCount(14);
        totals.Should().ContainSingle(t => t.Category == "Mercado" && t.YearlyTotal == 150m);
    }

    [Fact]
    public async Task GetInvestmentDiffs_ReturnsElevenAccountsAndNetPositionWithCorrectDiffs()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var januarySnapshots = await client.GetAsync("/api/v1/financial/investment-snapshots/2026/1");
        var january = await januarySnapshots.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var chaseSaveJan = january!.First(s => s.Account == "ChaseSave");
        await client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{chaseSaveJan.Id}", new UpdateInvestmentSnapshotValueDTO { Value = 1000m });
        var februarySnapshots = await client.GetAsync("/api/v1/financial/investment-snapshots/2026/2");
        var february = await februarySnapshots.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var chaseSaveFeb = february!.First(s => s.Account == "ChaseSave");
        await client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{chaseSaveFeb.Id}", new UpdateInvestmentSnapshotValueDTO { Value = 1200m });

        var response = await client.GetAsync("/api/v1/financial/yearly-summary/2026/investment-diffs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvestmentDiffsYearlyDTO>();
        result!.Accounts.Should().HaveCount(11);
        var chaseSave = result.Accounts.Single(a => a.Account == "ChaseSave");
        chaseSave.MonthlyValues[0].Should().Be(1000m);
        chaseSave.MonthlyValues[1].Should().Be(1200m);
        chaseSave.MonthlyDiffs[0].Should().Be(200m);
        result.NetPosition.MonthlyValues[0].Should().Be(1000m);
        result.NetPosition.FullYearNetChange.Should().Be(-1000m);
    }
}
