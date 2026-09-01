using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class TitheEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid GleisonId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000001");
    private static readonly Guid DizimoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000012");

    [Fact]
    public async Task GetTitheSummaryByMonth_WithIncomeAndDizimoExpense_ReturnsCalculatedFigures()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 3000m,
            BankId = BarclaysId
        });
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Tithe payment",
            Value = 200m,
            CategoryId = DizimoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });

        var response = await Client.GetAsync("/api/v1/financial/tithe/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CalculatedTithe.Should().Be(300m);
        summary.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryByMonth_WithDizimoExpenseCountsAsTitheFalse_ExcludesItFromTitheBalance()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 3000m,
            BankId = BarclaysId
        });
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Charitable offer",
            Value = 200m,
            CategoryId = DizimoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null,
            CountsAsTithe = false
        });

        var response = await Client.GetAsync("/api/v1/financial/tithe/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CalculatedTithe.Should().Be(300m);
        summary.TitheBalance.Should().Be(300m);
    }

    [Fact]
    public async Task GetTitheSummaryByMonth_WithBankLessIncomeAndOfferExpense_ReflectsBothInSameMonth()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 1000m,
            BankId = null,
            Description = "Chip ISA dividend"
        });
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Charitable offer",
            Value = 30m,
            CategoryId = DizimoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null,
            CountsAsTithe = false
        });

        var response = await Client.GetAsync("/api/v1/financial/tithe/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CalculatedTithe.Should().Be(100m);
        summary.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task GetTitheSummaryByMonth_WithNoData_ReturnsZeros()
    {
        var response = await Client.GetAsync("/api/v1/financial/tithe/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CalculatedTithe.Should().Be(0m);
        summary.TitheBalance.Should().Be(0m);
        summary.CarryForward.Should().BeNull();
    }

    // --- Carry-forward: dates computed relative to "today" since the effective-from boundary
    // auto-anchors to the real current month the first time it's resolved (see TitheServiceTests).

    private static DateOnly ThisMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private static DateOnly NextMonth => ThisMonth.AddMonths(1);

    [Fact]
    public async Task GetTitheSummaryByMonth_NextMonth_CarriesInThisMonthsUnpaidBalanceByDefault()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(ThisMonth.Year, ThisMonth.Month, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 1000m,
            BankId = BarclaysId
        });
        await Client.GetAsync($"/api/v1/financial/tithe/month/{ThisMonth.Year}/{ThisMonth.Month}");

        var response = await Client.GetAsync($"/api/v1/financial/tithe/month/{NextMonth.Year}/{NextMonth.Month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CarryForward.Should().NotBeNull();
        summary.CarryForward!.Amount.Should().Be(100m);
        summary.CarryForward.Included.Should().BeTrue();
        summary.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateCarryForwardInclusion_SetIncludedFalse_RemovesItFromTitheBalance()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(ThisMonth.Year, ThisMonth.Month, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 1000m,
            BankId = BarclaysId
        });
        await Client.GetAsync($"/api/v1/financial/tithe/month/{ThisMonth.Year}/{ThisMonth.Month}");
        await Client.GetAsync($"/api/v1/financial/tithe/month/{NextMonth.Year}/{NextMonth.Month}");

        var response = await Client.PutAsJsonAsync(
            $"/api/v1/financial/tithe/month/{NextMonth.Year}/{NextMonth.Month}/carry-forward",
            new TitheCarryForwardUpdateDTO { Included = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<TitheSummaryDTO>();
        summary!.CarryForward!.Included.Should().BeFalse();
        summary.TitheBalance.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateCarryForwardInclusion_MonthWithNoCarryForwardAvailable_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/v1/financial/tithe/month/2026/7/carry-forward",
            new TitheCarryForwardUpdateDTO { Included = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCarryForwardInclusion_NullBody_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync<object?>("/api/v1/financial/tithe/month/2026/7/carry-forward", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
