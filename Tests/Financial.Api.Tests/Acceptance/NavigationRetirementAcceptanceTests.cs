using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class NavigationRetirementAcceptanceTests : ApiEndpointTests
{
    private const string ZetaCategoryId = "0a710000-0000-0000-0000-000000000001";
    private const string AlphaCategoryId = "0a710000-0000-0000-0000-000000000002";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "0a710000-0000-0000-0000-00000000000a", "Name": "Zeta pots", "Template": "Pots", "Currency": "GBP", "IsActive": false, "OpeningBalance": null, "RoundUp": null, "Pots": [], "CategoryId": "{{ZetaCategoryId}}" },
            { "Id": "0a710000-0000-0000-0000-00000000000b", "Name": "Alpha pots", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [], "CategoryId": "{{AlphaCategoryId}}" },
            { "Id": "0a710000-0000-0000-0000-00000000000c", "Name": "Zeta settlement", "Template": "Settlement", "Currency": "GBP", "IsActive": false, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["GBP"], "OtherParticipant": "Zeta" },
            { "Id": "0a710000-0000-0000-0000-00000000000d", "Name": "Alpha settlement", "Template": "Settlement", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["GBP"], "OtherParticipant": "Alpha" }
          ],
          "Categories": [
            { "Id": "{{ZetaCategoryId}}", "Name": "Zeta", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{AlphaCategoryId}}", "Name": "Alpha", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Version": 5
        }
        """;

    public NavigationRetirementAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-04")]
    public async Task Accounts_ReplacesPage_FirstPotsAndSettlementInFileOrder()
    {
        var accounts = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!;

        using (new AssertionScope())
        {
            accounts.Single(account => account.Name == "Zeta pots").ReplacesPage.Should().Be("reserva");
            accounts.Single(account => account.Name == "Alpha pots").ReplacesPage.Should().BeNull();
            accounts.Single(account => account.Name == "Zeta settlement").ReplacesPage.Should().Be("controle-mae");
            accounts.Single(account => account.Name == "Alpha settlement").ReplacesPage.Should().BeNull();
        }
    }
}
