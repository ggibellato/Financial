using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class AccountsByTemplateAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "6d2e0000-0000-0000-0000-000000000001";
    private const string AmexId = "6d2e0000-0000-0000-0000-000000000002";
    private const string OldPotsId = "6d2e0000-0000-0000-0000-000000000003";
    private const string ItauId = "6d2e0000-0000-0000-0000-000000000004";
    private const string FlightsId = "6d2e0000-0000-0000-0000-000000000301";
    private const string MercadoId = "6d2e0000-0000-0000-0000-000000000101";
    private const string OldPotsCategoryId = "6d2e0000-0000-0000-0000-000000000102";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{OldPotsId}}", "Name": "Old pots", "Template": "Pots", "Currency": "GBP", "IsActive": false, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{FlightsId}}", "Name": "Flights", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{OldPotsCategoryId}}" },
            { "Id": "{{ItauId}}", "Name": "Itaú", "Template": "Bank", "Currency": "BRL", "IsActive": true, "OpeningBalance": { "Amount": 500, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{OldPotsCategoryId}}", "Name": "Old pots", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "CardStatements": [
            { "Id": "6d2e0000-0000-0000-0000-000000000501", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 8, "IsPaid": false, "DueDate": null }
          ],
          "ReserveMovements": [
            { "Id": "6d2e0000-0000-0000-0000-000000000401", "BucketId": "{{FlightsId}}", "Amount": 704.08, "Date": "2026-10-05", "Description": "Split", "IncomeId": null }
          ],
          "Expenses": [
            { "Id": "6d2e0000-0000-0000-0000-000000000201", "Date": "2026-09-10", "Description": "September shop", "Value": 154.04, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-10", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": false },
            { "Id": "6d2e0000-0000-0000-0000-000000000202", "Date": "2026-10-03", "Description": "October shop", "Value": 586.08, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-03", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": false },
            { "Id": "6d2e0000-0000-0000-0000-000000000203", "Date": "2026-10-02", "Description": "Settled lunch", "Value": 7, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-15", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": false }
          ],
          "Version": 4
        }
        """;

    public AccountsByTemplateAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private async Task<AccountDetailDTO> AccountAsync(string accountId, int month) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{accountId}/2026/{month}"))!;

    [Fact]
    [Trait("AC", "P60-F14-accounts-page-by-template-and-card-totals-03")]
    public async Task AccountDetail_CardInTwoMonths_KeepsTotalOwed()
    {
        var september = await AccountAsync(AmexId, 9);
        var october = await AccountAsync(AmexId, 10);

        using (new AssertionScope())
        {
            september.Balance.Should().Be(-154.04m);
            october.Balance.Should().Be(-740.12m);
            september.TotalOwed.Should().Be(740.12m);
            october.TotalOwed.Should().Be(740.12m);
        }
    }

    private async Task<List<AccountTemplateGroupDTO>> TemplateGroupsAsync(int month) =>
        (await Client.GetFromJsonAsync<List<AccountTemplateGroupDTO>>($"{Api}/accounts/groups/by-template/2026/{month}"))!;

    [Fact]
    [Trait("AC", "P60-F14-accounts-page-by-template-and-card-totals-01")]
    public async Task TemplateGroups_Month_ReturnsGroupsInTemplateOrderWithoutEmptyOnes()
    {
        var september = await TemplateGroupsAsync(9);
        var october = await TemplateGroupsAsync(10);

        using (new AssertionScope())
        {
            september.Select(group => group.TemplateName).Should().Equal("Bank", "CreditCard");
            october.Select(group => group.TemplateName).Should().Equal("Bank", "CreditCard", "Pots");
            october[0].Members.Select(member => member.Name).Should().Equal("Barclays", "Itaú");
            october[0].Totals.Select(total => (total.Currency, total.Total)).Should().Equal(("GBP", 993m), ("BRL", 500m));
            october[1].Members.Single().TotalOwed.Should().Be(740.12m);
            october[1].Totals.Select(total => (total.Currency, total.Total)).Should().Equal(("GBP", 740.12m));
            var oldPots = october[2].Members.Single();
            oldPots.Name.Should().Be("Old pots");
            oldPots.IsActive.Should().BeFalse();
            oldPots.Balance.Should().Be(704.08m);
            october[2].Totals.Select(total => (total.Currency, total.Total)).Should().Equal(("GBP", 704.08m));
        }
    }

    [Fact]
    [Trait("AC", "P60-F14-accounts-page-by-template-and-card-totals-02")]
    public async Task TemplateGroups_Card_ReturnsNextStatementAndTotalOwed()
    {
        var september = (await TemplateGroupsAsync(9))[1].Members.Single();
        var octoberGroups = await TemplateGroupsAsync(10);
        var october = octoberGroups[1].Members.Single();

        using (new AssertionScope())
        {
            september.Name.Should().Be("BA Amex");
            september.NextStatement.Should().BeEquivalentTo(new AccountNextStatementDTO { Year = 2026, Month = 9, OutstandingTotal = 154.04m });
            september.TotalOwed.Should().Be(740.12m);
            october.NextStatement.Should().BeEquivalentTo(september.NextStatement);
            october.TotalOwed.Should().Be(740.12m);
            octoberGroups[0].Members.Should().OnlyContain(member => member.NextStatement == null);
        }
    }

    [Fact]
    public async Task TemplateGroups_MonthOutOfRange_Returns400()
    {
        var response = await Client.GetAsync($"{Api}/accounts/groups/by-template/2026/13");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AccountDetail_Bank_HasNoTotalOwed()
    {
        (await AccountAsync(BarclaysId, 10)).TotalOwed.Should().BeNull();
    }
}
