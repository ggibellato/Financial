using Financial.Api.Tests.TestHelpers;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class AnnualSummaryEndpointsTests
{
    [Fact]
    public async Task GetExpenseCategoryTotals_RouteRemoved_Returns404()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/annual-summary/2026/expense-categories");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIncomeSummary_RouteRemoved_Returns404()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/annual-summary/2026/income-summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvestmentDiffs_RouteRemoved_Returns404()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/annual-summary/2026/investment-diffs");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistoricSummaryAverages_MergesIncomeIntoMatchingYearAndOmitsItFromYearsWithoutIncome()
    {
        var pinnedNow = new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);
        var currentYear = pinnedNow.Year;
        var monthsToAverage = pinnedNow.Month - 1;
        var pastYear = currentYear - 1;

        await using var factory = new ApiTestFactory(timeProviderOverride: new FakeTimeProvider(pinnedNow));
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(pastYear, 6, 5),
            Description = "Past year groceries",
            Value = 120m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(currentYear, 1, 5),
            Description = "January groceries",
            Value = 100m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(currentYear, 3, 5),
            Description = "March groceries",
            Value = 50m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(currentYear, 1, 1),
            IncomeSource = "Gleison",
            GrossValue = 3200m,
            NetValue = 2450m,
            Bank = "Barclays"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(currentYear, 1, 8),
            IncomeSource = "Ariana",
            GrossValue = 400m,
            NetValue = 350m,
            Bank = "Chase"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(currentYear, 3, 1),
            IncomeSource = "DividendoJuros",
            GrossValue = null,
            NetValue = 15.50m,
            Bank = "Trading212"
        });

        var response = await client.GetAsync($"/api/v1/financial/annual-summary/{currentYear}/historic-summary-averages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CategoryAnnualGroupValueDTO>>();
        result.Should().HaveCount(2);
        result![0].Year.Should().Be(currentYear);
        result[1].Year.Should().Be(pastYear);

        // Current year: Mercado's total (Jan 100 + Mar 50 = 150) divides by the completed months so
        // far this year (current month - 1), same divisor as income, giving 150/monthsToAverage = 25 -
        // not an average over only the months with a recorded entry.
        var expectedSalary = Math.Round(3600m / monthsToAverage, 2);
        var expectedSalaryAfterTaxes = Math.Round(2800m / monthsToAverage, 2);
        var expectedDividendoJuros = Math.Round(15.50m / monthsToAverage, 2);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Mercado" && a.Value == 25m);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Salary" && a.Value == expectedSalary);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Salary after taxes" && a.Value == expectedSalaryAfterTaxes);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Tax difference" && a.Value == expectedSalary - expectedSalaryAfterTaxes);
        result[0].AnnualAverages.Should().ContainSingle(a => a.Category == "Dividendo/Juros" && a.Value == expectedDividendoJuros);

        // Past year has expenses but no income, so no income rows should be merged in for that year.
        // A full past year always divides by 12 regardless of how many months have a recorded entry:
        // 120 / 12 = 10, not 120 (the June value) as a per-active-month average would give.
        result[1].AnnualAverages.Should().ContainSingle(a => a.Category == "Mercado" && a.Value == 10m);
        result[1].AnnualAverages.Should().NotContain(a => a.Category == "Salary");
    }

    [Fact]
    public async Task GetCategoryTotals_ReturnsCombinedShapeWithComputedTotalDespesasAndResultado()
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
            Date = new DateOnly(2026, 1, 5),
            Description = "January investing",
            Value = 30m,
            Category = "Investimento",
            PaymentSource = "Barclays",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 1, 1),
            IncomeSource = "Gleison",
            GrossValue = 1000m,
            NetValue = 800m,
            Bank = "Barclays"
        });
        await client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 1, 1),
            IncomeSource = "DividendoJuros",
            GrossValue = null,
            NetValue = 20m,
            Bank = "Trading212"
        });

        var response = await client.GetAsync("/api/v1/financial/annual-summary/2026/category-totals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryTotalsAnnualDTO>();
        result!.CategoryTotals.Should().HaveCount(14);
        result.IncomeSummary.SalaryAfterTaxesMonthly[0].Should().Be(800m);
        result.TotalDespesasMonthly[0].Should().Be(130m);
        result.TotalDespesasAnnualTotal.Should().Be(130m);
        // Resultado = SalaryAfterTaxes(800) - TotalDespesas(130) + Investimento(30) = 700, excluding Dividendo/Juros.
        result.ResultadoMonthly[0].Should().Be(700m);
        result.ResultadoAnnualTotal.Should().Be(700m);
    }

    [Fact]
    public async Task GetCategoryTotals_NoRecordedData_ReturnsAllZeroSeries()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/annual-summary/2026/category-totals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryTotalsAnnualDTO>();
        result!.CategoryTotals.Should().OnlyContain(c => c.AnnualTotal == 0m);
        result.TotalDespesasMonthly.Should().OnlyContain(v => v == 0m);
        result.ResultadoMonthly.Should().OnlyContain(v => v == 0m);
    }

    [Fact]
    public async Task GetInvestmentAnnualResult_ReturnsOkMatchingSeededSnapshots()
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

        var response = await client.GetAsync("/api/v1/financial/annual-summary/2026/investment-annual-result");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvestmentAnnualResultDTO>();
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
    public async Task GetInvestmentAnnualResult_NoData_ReturnsEmptyAccountsArray()
    {
        var pastYear = DateTime.UtcNow.Year - 5;
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/financial/annual-summary/{pastYear}/investment-annual-result");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvestmentAnnualResultDTO>();
        result!.Accounts.Should().BeEmpty();
        result.NetPosition.MonthlyValues.Should().OnlyContain(v => v == 0m);
    }
}
