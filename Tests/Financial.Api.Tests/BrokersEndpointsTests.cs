using Financial.Investment.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class BrokersEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task CreateBroker_ValidRequest_ReturnsOkWithActiveBroker()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var broker = await response.Content.ReadFromJsonAsync<BrokerDTO>();
        broker.Should().NotBeNull();
        broker!.Name.Should().Be("TestBrokerA");
        broker.Currency.Should().Be("BRL");
        broker.Status.Should().Be("Active");
        broker.PortfolioCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateBroker_DuplicateName_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));

        var response = await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TestBrokerA").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateBroker_NameAlreadyUsedBySeededHistoricBroker_ReturnsConflict()
    {
        // The shared test fixture already seeds a Historic broker named "XPI" - uniqueness must span
        // both scopes, not just Active.
        var response = await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("XPI"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBroker_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/brokers", new BrokerCreateDTO { Name = "   ", Currency = "BRL" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBrokers_ReturnsCreatedBrokersAlongsideSeededOnes()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerB"));

        var response = await Client.GetAsync("/api/v1/financial/brokers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var brokers = await response.Content.ReadFromJsonAsync<List<BrokerDTO>>();
        brokers!.Select(b => b.Name).Should().Contain(["TestBrokerA", "TestBrokerB"]);
    }

    [Fact]
    public async Task UpdateBroker_ValidRequest_PersistsNewNameAndCurrency()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));

        var response = await Client.PutAsJsonAsync(
            "/api/v1/financial/brokers/TestBrokerA",
            new BrokerUpdateDTO { Name = "TestBrokerRenamed", Currency = "USD" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var broker = await response.Content.ReadFromJsonAsync<BrokerDTO>();
        broker!.Name.Should().Be("TestBrokerRenamed");
        broker.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task UpdateBroker_UnknownBroker_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/v1/financial/brokers/Nope",
            new BrokerUpdateDTO { Name = "New Name", Currency = "BRL" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBroker_NameCollidesWithAnotherBroker_ReturnsConflict()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerB"));

        var response = await Client.PutAsJsonAsync(
            "/api/v1/financial/brokers/TestBrokerA",
            new BrokerUpdateDTO { Name = "TestBrokerB", Currency = "BRL" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteBroker_ActiveAndEmpty_ReturnsNoContentAndMovesToHistoric()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/brokers", ValidBrokerRequest("TestBrokerA"));

        var response = await Client.DeleteAsync("/api/v1/financial/brokers/TestBrokerA");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var brokers = await (await Client.GetAsync("/api/v1/financial/brokers")).Content.ReadFromJsonAsync<List<BrokerDTO>>();
        brokers.Should().ContainSingle(b => b.Name == "TestBrokerA" && b.Status == "Historic");
    }

    [Fact]
    public async Task DeleteBroker_UnknownBroker_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/v1/financial/brokers/Nope");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBroker_WithPortfolios_ReturnsConflictWithMessage()
    {
        // The shared test fixture seeds an Active "XPI" broker with a "Default" portfolio already
        // holding an asset - the only broker in this fixture with a non-empty portfolio.
        var response = await Client.DeleteAsync("/api/v1/financial/brokers/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("still has portfolios");
    }

    private static BrokerCreateDTO ValidBrokerRequest(string name) => new() { Name = name, Currency = "BRL" };
}
