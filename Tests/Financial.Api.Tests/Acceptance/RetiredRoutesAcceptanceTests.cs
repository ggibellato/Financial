using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class RetiredRoutesAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string MaeId = "5e771e00-0000-0000-0000-000000000002";
    private const string EntryId = "5e771e00-0000-0000-0000-000000000101";
    private const string ReservaId = "5e771e00-0000-0000-0000-000000000003";
    private const string PotId = "5e771e00-0000-0000-0000-000000000201";
    private const string MovementId = "5e771e00-0000-0000-0000-000000000301";
    private const string BankId = "5e771e00-0000-0000-0000-000000000004";
    private const string AdjustmentId = "5e771e00-0000-0000-0000-000000000401";
    private const string CardId = "5e771e00-0000-0000-0000-000000000005";
    private const string StatementId = "5e771e00-0000-0000-0000-000000000501";
    private const string ChaseId = "5e771e00-0000-0000-0000-000000000006";
    private const string TransferId = "5e771e00-0000-0000-0000-000000000601";
    private const string ExpenseId = "5e771e00-0000-0000-0000-000000000701";
    private const string CategoryId = "5e771e00-0000-0000-0000-000000000801";
    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BankId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{CardId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{MaeId}}", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{PotId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": null }
          ],
          "Categories": [
            { "Id": "{{CategoryId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "{{ExpenseId}}", "Date": "2026-09-04", "Description": "Coffee", "Value": 3, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": "{{ChaseId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "BalanceAdjustments": [
            { "Id": "{{AdjustmentId}}", "Date": "2026-09-10", "AccountId": "{{BankId}}", "TargetBalance": 10, "Delta": 10, "Note": null }
          ],
          "CardStatements": [
            { "Id": "{{StatementId}}", "CreditCardId": "{{CardId}}", "Year": 2026, "Month": 9, "IsPaid": false, "DueDate": null }
          ],
          "Transfers": [
            { "Id": "{{TransferId}}", "Date": "2026-09-05", "SourceAccountId": "{{BankId}}", "DestinationAccountId": "{{ChaseId}}", "Amount": 20 }
          ],
          "ReserveMovements": [
            { "Id": "{{MovementId}}", "BucketId": "{{PotId}}", "Amount": 50, "Date": "2026-09-01", "Description": "Split", "IncomeId": null }
          ],
          "Version": 5
        }
        """;

    public RetiredRoutesAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private static HttpRequestMessage EmptyBodyRequest(string method, string path) =>
        new(new HttpMethod(method), $"{Api}/{path}") { Content = method == "GET" ? null : JsonContent.Create<object?>(null) };

    [Theory]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-05")]
    [InlineData("POST", "controle-mae/entries")]
    [InlineData("GET", "controle-mae/entries/from/2026-01-01")]
    [InlineData("GET", "controle-mae/entries/totals")]
    [InlineData("PUT", $"controle-mae/entries/{EntryId}/values")]
    [InlineData("DELETE", $"controle-mae/entries/{EntryId}")]
    [InlineData("POST", "reserve/income-split")]
    [InlineData("POST", "reserve/withdrawals")]
    [InlineData("GET", "reserve/balances")]
    [InlineData("GET", "reserve/movements")]
    [InlineData("GET", "reserve-buckets")]
    [InlineData("GET", "reserve-buckets/split-status")]
    [InlineData("GET", "banks")]
    [InlineData("POST", "banks")]
    [InlineData("PUT", $"banks/{BankId}")]
    [InlineData("DELETE", $"banks/{BankId}")]
    [InlineData("PUT", $"banks/{BankId}/opening-balance")]
    [InlineData("GET", "banks/month/2026/9/balances")]
    [InlineData("GET", $"banks/{BankId}/adjustments")]
    [InlineData("POST", $"banks/{BankId}/adjustments")]
    [InlineData("PUT", $"banks/{BankId}/adjustments/{AdjustmentId}")]
    [InlineData("DELETE", $"banks/{BankId}/adjustments/{AdjustmentId}")]
    [InlineData("GET", "credit-cards")]
    [InlineData("POST", "credit-cards")]
    [InlineData("PUT", $"credit-cards/{CardId}")]
    [InlineData("DELETE", $"credit-cards/{CardId}")]
    [InlineData("POST", "transfers")]
    [InlineData("PUT", $"transfers/{TransferId}")]
    [InlineData("DELETE", $"transfers/{TransferId}")]
    [InlineData("GET", "transfers/month/2026/9")]
    [InlineData("GET", $"transfers/bank/{BankId}")]
    [InlineData("GET", "card-statements/2026/9")]
    [InlineData("GET", "expenses/month/2026/9")]
    [InlineData("GET", "expenses/month/2026/9/unpaid-card-charges")]
    public async Task RetiredRoute_Returns404(string method, string path)
    {
        var response = await Client.SendAsync(EmptyBodyRequest(method, path));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-05")]
    [InlineData("GET", "accounts", HttpStatusCode.OK)]
    [InlineData("GET", "accounts/groups/by-template/2026/9", HttpStatusCode.OK)]
    [InlineData("GET", $"accounts/{MaeId}/2026/9", HttpStatusCode.OK)]
    [InlineData("GET", $"accounts/{MaeId}/transactions/2026/9", HttpStatusCode.OK)]
    [InlineData("POST", $"accounts/{MaeId}/settlement-entries", HttpStatusCode.BadRequest)]
    [InlineData("PUT", $"accounts/{MaeId}/settlement-entries/{EntryId}", HttpStatusCode.BadRequest)]
    [InlineData("POST", $"accounts/{MaeId}/settlements", HttpStatusCode.BadRequest)]
    [InlineData("GET", $"accounts/{ReservaId}/pots/2026/9", HttpStatusCode.OK)]
    [InlineData("POST", $"accounts/{ReservaId}/deposits", HttpStatusCode.BadRequest)]
    [InlineData("POST", $"accounts/{ReservaId}/withdrawals", HttpStatusCode.BadRequest)]
    [InlineData("PUT", $"reserve/movements/{MovementId}", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", $"reserve/movements/{MovementId}", HttpStatusCode.OK)]
    [InlineData("GET", $"accounts/{BankId}/2026/9", HttpStatusCode.OK)]
    [InlineData("GET", $"accounts/{BankId}/transactions/2026/9", HttpStatusCode.OK)]
    [InlineData("PUT", $"accounts/{BankId}", HttpStatusCode.BadRequest)]
    [InlineData("POST", $"accounts/{BankId}/adjustments", HttpStatusCode.BadRequest)]
    [InlineData("PUT", $"accounts/{BankId}/adjustments/{AdjustmentId}", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", $"accounts/{BankId}/adjustments/{AdjustmentId}", HttpStatusCode.NoContent)]
    [InlineData("GET", $"accounts/{CardId}/2026/9", HttpStatusCode.OK)]
    [InlineData("GET", $"accounts/{CardId}/statements", HttpStatusCode.OK)]
    [InlineData("PUT", $"accounts/{CardId}/statements/2026/9/due-date", HttpStatusCode.BadRequest)]
    [InlineData("POST", $"card-statements/{StatementId}/mark-paid", HttpStatusCode.BadRequest)]
    [InlineData("POST", $"card-statements/{StatementId}/unmark-paid", HttpStatusCode.Conflict)]
    [InlineData("POST", "accounts/transfers", HttpStatusCode.BadRequest)]
    [InlineData("PUT", $"accounts/transfers/{TransferId}", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", $"accounts/transfers/{TransferId}", HttpStatusCode.OK)]
    [InlineData("GET", "accounts/transactions/2026/9", HttpStatusCode.OK)]
    [InlineData("GET", "expenses/month/2026/9/category-totals", HttpStatusCode.OK)]
    [InlineData("POST", "expenses", HttpStatusCode.BadRequest)]
    [InlineData("PUT", $"expenses/{ExpenseId}", HttpStatusCode.BadRequest)]
    [InlineData("POST", $"expenses/{ExpenseId}/move-to-statement", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", $"expenses/{ExpenseId}", HttpStatusCode.OK)]
    public async Task KeptRoute_StillAnswers(string method, string path, HttpStatusCode expected)
    {
        var response = await Client.SendAsync(EmptyBodyRequest(method, path));

        response.StatusCode.Should().Be(expected);
    }
}
