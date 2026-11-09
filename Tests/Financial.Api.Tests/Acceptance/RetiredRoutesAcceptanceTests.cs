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
    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{MaeId}}", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" }
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
    public async Task KeptRoute_StillAnswers(string method, string path, HttpStatusCode expected)
    {
        var response = await Client.SendAsync(EmptyBodyRequest(method, path));

        response.StatusCode.Should().Be(expected);
    }
}
