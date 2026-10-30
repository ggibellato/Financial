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
    private const string MercadoId = "6d2e0000-0000-0000-0000-000000000101";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
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

    [Fact]
    public async Task AccountDetail_Bank_HasNoTotalOwed()
    {
        (await AccountAsync(BarclaysId, 10)).TotalOwed.Should().BeNull();
    }
}
