using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class InvestmentAccountsEndpointsTests
{
    [Fact]
    public async Task GetInvestmentAccounts_ReturnsTheElevenSeededAccountsWithCorrectFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/investment-accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();
        accounts.Should().HaveCount(11);
        accounts.Should().ContainSingle(a => a.Name == "ChaseSave" && a.IsActive && !a.IsLiability && a.Id != Guid.Empty);
        accounts.Should().ContainSingle(a => a.Name == "PlatinumVisa8003" && a.IsActive && a.IsLiability);
    }

    [Fact]
    public async Task GetInvestmentAccounts_RequiresNoParameters_AndReturnsFullUnfilteredList()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/investment-accounts");
        var accounts = await response.Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();

        // All 11 seeded accounts come back regardless of IsActive/IsLiability value.
        accounts.Should().HaveCount(11);
        accounts.Should().OnlyContain(a => a.IsActive);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task InvestmentAccounts_UnsupportedVerbs_DoNotSucceed(string method)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/financial/investment-accounts");

        var response = await client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
