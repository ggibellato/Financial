using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class PotsTemplateAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "9a1c0000-0000-0000-0000-000000000001";
    private const string ReservaId = "9a1c0000-0000-0000-0000-000000000002";
    private const string HolidaysId = "9a1c0000-0000-0000-0000-000000000003";
    private const string InvestimentoId = "9a1c0000-0000-0000-0000-000000000101";
    private const string FeriasId = "9a1c0000-0000-0000-0000-000000000102";
    private const string FlightsId = "9a1c0000-0000-0000-0000-000000000103";
    private const string ReservaCategoryId = "9a1c0000-0000-0000-0000-000000000301";
    private const string HolidaysCategoryId = "9a1c0000-0000-0000-0000-000000000302";
    private const string MercadoId = "9a1c0000-0000-0000-0000-000000000303";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 },
              { "Id": "{{FeriasId}}", "Name": "Ferias", "IsActive": false, "SplitPercentage": 0 }
            ], "CategoryId": "{{ReservaCategoryId}}" },
            { "Id": "{{HolidaysId}}", "Name": "Holidays", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{FlightsId}}", "Name": "Flights", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{HolidaysCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{HolidaysCategoryId}}", "Name": "Holidays", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "ReserveMovements": [
            { "Id": "9a1c0000-0000-0000-0000-000000000201", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2024-03-01", "Description": "Split", "IncomeId": null },
            { "Id": "9a1c0000-0000-0000-0000-000000000202", "BucketId": "{{FeriasId}}", "Amount": 200, "Date": "2026-09-10", "Description": "Split", "IncomeId": null },
            { "Id": "9a1c0000-0000-0000-0000-000000000203", "BucketId": "{{InvestimentoId}}", "Amount": -75.5, "Date": "2026-10-12", "Description": "Withdrawal", "IncomeId": null },
            { "Id": "9a1c0000-0000-0000-0000-000000000204", "BucketId": "{{FlightsId}}", "Amount": 900, "Date": "2026-10-01", "Description": "Other account", "IncomeId": null }
          ],
          "Version": 4
        }
        """;

    public PotsTemplateAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private async Task<decimal> BalanceAsync(string accountId, int month) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{accountId}/2026/{month}"))!.Balance;

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-02")]
    public async Task ReservaBalance_IsTheSumOfItsPotBalances_AndCountsOnlyItsOwnPotsUpToMonthEnd()
    {
        var potBalances = (await Client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>($"{Api}/reserve/balances"))!;

        using (new AssertionScope())
        {
            potBalances.Select(pot => (pot.BucketName, pot.Balance)).Should().Equal(("Investimento", 424.5m), ("Ferias", 200m));
            (await BalanceAsync(ReservaId, 10)).Should().Be(624.5m).And.Be(potBalances.Sum(pot => pot.Balance));
            (await BalanceAsync(ReservaId, 9)).Should().Be(700m);
            (await BalanceAsync(HolidaysId, 10)).Should().Be(900m);
            (await BalanceAsync(BarclaysId, 10)).Should().Be(1000m);
        }
    }

    [Fact]
    public async Task ReservaTransactions_MonthWithMovements_ListsNoRows()
    {
        var rows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{ReservaId}/transactions/2026/10");

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteReserva_OwnsPots_Returns409WithTheDeactivateMessageAndKeepsIt()
    {
        var response = await Client.DeleteAsync($"{Api}/accounts/{ReservaId}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            problem!.Detail.Should().Be("Reserva has transactions and cannot be deleted. Deactivate it instead.");
            accounts!.Should().ContainSingle(account => account.Name == "Reserva").Which.HasRecords.Should().BeTrue();
        }
    }

    [Fact]
    public async Task DeleteReservasCategory_SetOnTheAccount_Returns409AndKeepsIt()
    {
        var response = await Client.DeleteAsync($"{Api}/categories/{ReservaCategoryId}");
        var categories = await Client.GetFromJsonAsync<List<CategoryDTO>>($"{Api}/categories");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            categories!.Should().ContainSingle(category => category.Name == "Reserva").Which.HasReferences.Should().BeTrue();
        }
    }
}
