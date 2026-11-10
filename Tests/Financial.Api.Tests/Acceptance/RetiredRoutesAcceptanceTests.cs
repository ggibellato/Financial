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
    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BankId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{MaeId}}", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{PotId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": null }
          ],
          "BalanceAdjustments": [
            { "Id": "{{AdjustmentId}}", "Date": "2026-09-10", "AccountId": "{{BankId}}", "TargetBalance": 10, "Delta": 10, "Note": null }
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
    public async Task KeptRoute_StillAnswers(string method, string path, HttpStatusCode expected)
    {
        var response = await Client.SendAsync(EmptyBodyRequest(method, path));

        response.StatusCode.Should().Be(expected);
    }
}
