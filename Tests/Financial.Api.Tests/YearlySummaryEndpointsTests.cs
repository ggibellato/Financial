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
        chaseSave.MonthlyDiffs[0].Should().BeNull();
        chaseSave.MonthlyDiffs[1].Should().Be(200m);
        result.NetPosition.MonthlyValues[0].Should().Be(1000m);
        result.NetPosition.FullYearNetChange.Should().Be(-1000m);
    }

    [Fact]
    public async Task GetIncomeSummary_ReturnsFiguresMatchingSeededIncomeAcrossSourcesAndMonths()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 1, 1),
            IncomeSource = "Gleison",
            GrossValue = 3200m,
            NetValue = 2450m,
            Bank = "Barclays"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 1, 8),
            IncomeSource = "Ariana",
            GrossValue = 400m,
            NetValue = 350m,
            Bank = "Chase"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 3, 1),
            IncomeSource = "DividendoJuros",
            GrossValue = null,
            NetValue = 15.50m,
            Bank = "Trading212"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 4, 1),
            IncomeSource = "Lottery",
            GrossValue = null,
            NetValue = 500m,
            Bank = "Chase"
        });

        var response = await client.GetAsync("/api/v1/financial/yearly-summary/2026/income-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeYearlySummaryDTO>();
        result!.SalaryMonthly[0].Should().Be(3600m);
        result.SalaryAfterTaxesMonthly[0].Should().Be(2800m);
        result.TaxDifferenceMonthly[0].Should().Be(800m);
        result.DividendoJurosMonthly[2].Should().Be(15.50m);
        result.SalaryMonthly[3].Should().Be(0m);
        result.SalaryYearlyTotal.Should().Be(3600m);
    }

    [Fact]
    public async Task GetHistoricCategoriesAverages_MergesIncomeIntoMatchingYearAndOmitsItFromYearsWithoutIncome()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2025, 6, 5),
            Description = "2025 groceries",
            Value = 120m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });
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
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 1, 1),
            IncomeSource = "Gleison",
            GrossValue = 3200m,
            NetValue = 2450m,
            Bank = "Barclays"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 1, 8),
            IncomeSource = "Ariana",
            GrossValue = 400m,
            NetValue = 350m,
            Bank = "Chase"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 3, 1),
            IncomeSource = "DividendoJuros",
            GrossValue = null,
            NetValue = 15.50m,
            Bank = "Trading212"
        });

        var response = await client.GetAsync("/api/v1/financial/yearly-summary/2026/historic-categories-averages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CategoryAnnualAverageDTO>>();
        result.Should().HaveCount(2);
        result![0].Year.Should().Be(2026);
        result[1].Year.Should().Be(2025);

        // 2026: Mercado's two recorded months (Jan 100 + Mar 50) average to 75; income is combined
        // (Gleison + Ariana) per month before averaging, over the single recorded month each.
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Mercado" && a.Average == 75m);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Salary" && a.Average == 3600m);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Salary after taxes" && a.Average == 2800m);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Tax difference" && a.Average == 800m);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Dividendo/Juros" && a.Average == 15.50m);

        // 2025 has expenses but no income, so no income rows should be merged in for that year.
        result[1].AnnualAverages.Should().ContainSingle(a => a.Category == "Mercado" && a.Average == 120m);
        result[1].AnnualAverages.Should().NotContain(a => a.Category == "Salary");
    }
}
