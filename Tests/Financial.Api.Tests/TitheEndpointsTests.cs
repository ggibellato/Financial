using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
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
    public async Task UpdateCarryForwardInclusion_MonthWithNoCarryForwardAvailable_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/v1/financial/tithe/month/2026/7/carry-forward",
            new TitheCarryForwardUpdateDTO { Included = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
