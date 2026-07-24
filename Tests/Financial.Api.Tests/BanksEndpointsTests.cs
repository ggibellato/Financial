using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class BanksEndpointsTests
{
    [Fact]
    public async Task GetBanks_ReturnsTheThreeSeededBanksWithCorrectRoundUpFlags()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banks = await response.Content.ReadFromJsonAsync<List<BankDTO>>();
        banks.Should().HaveCount(3);
        banks.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
        banks.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
        banks.Should().ContainSingle(b => b.Name == "Chase" && b.RoundUpEnabled);
    }
}
