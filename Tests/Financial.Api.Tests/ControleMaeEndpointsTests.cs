using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ControleMaeEndpointsTests : ApiEndpointTests
{
    private const decimal DefaultRate = 1.5m;

    private const string Seed = """
        {
          "Accounts": [
            { "Id": "6d0e0000-0000-0000-0000-000000000001", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" }
          ],
          "SettlementTransactions": [],
          "Version": 5
        }
        """;

    public ControleMaeEndpointsTests() : base(new StubExchangeRateProvider(DefaultRate))
    {
        ReplaceCashFlowDataBeforeStart(Seed);
    }

    private static ApiTestFactory CreateFactory(decimal? rate)
    {
        var factory = new ApiTestFactory(new StubExchangeRateProvider(rate));
        factory.ReplaceCashFlowData(Seed);
        return factory;
    }

    [Fact]
    public async Task CreateEntry_ValidRequest_ReturnsOkWithBothCurrenciesPopulated()
    {
        await using var factory = CreateFactory(0.146m);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "School supplies",
            Note = "Term start",
            SourceCurrency = "BRL",
            SourceValue = 350m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();
        entry.Should().NotBeNull();
        entry!.BrlValue.Should().Be(350m);
        entry.GbpValue.Should().Be(51.1m);
    }

    [Fact]
    public async Task CreateEntry_WhenRateLookupFails_ReturnsOkWithOnlyEnteredCurrency()
    {
        await using var factory = CreateFactory(null);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Medical appointment",
            SourceCurrency = "GBP",
            SourceValue = 40m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();
        entry!.GbpValue.Should().Be(40m);
        entry.BrlValue.Should().BeNull();
    }

    [Fact]
    public async Task CreateEntry_UnrecognizedCurrency_ReturnsBadRequestWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Test",
            SourceCurrency = "USD",
            SourceValue = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Currency");
    }

    [Fact]
    public async Task CreateEntry_FutureDate_ReturnsBadRequestWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = TestClock.Today.AddDays(1),
            Description = "Future",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("future");
    }

    [Fact]
    public async Task GetEntriesFromDate_ReturnsOnlyEntriesOnOrAfterDate()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 6, 10),
            Description = "June entry",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });
        await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "July entry",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });

        var response = await Client.GetAsync("/api/v1/financial/controle-mae/entries/from/2026-07-01");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<MaeLedgerEntryDTO>>();
        entries.Should().ContainSingle(e => e.Description == "July entry");
    }

    [Fact]
    public async Task GetTotals_SumsBrlAndGbpAcrossAllEntries()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2020, 1, 1),
            Description = "Old entry",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });
        await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Recent entry",
            SourceCurrency = "BRL",
            SourceValue = 50m
        });

        var response = await Client.GetAsync("/api/v1/financial/controle-mae/entries/totals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var totals = await response.Content.ReadFromJsonAsync<MaeLedgerTotalsDTO>();
        totals!.TotalBrlValue.Should().Be(150m);
    }

    [Fact]
    public async Task UpdateEntryValues_ExistingId_ReturnsOkAndUpdatesOnlyValues()
    {
        await using var factory = CreateFactory(null);
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 16),
            Description = "Medical appointment",
            SourceCurrency = "GBP",
            SourceValue = 40m
        });
        var createdEntry = await created.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/controle-mae/entries/{createdEntry!.Id}/values", new MaeLedgerEntryValuesUpdateDTO
        {
            BrlValue = 320.50m,
            GbpValue = 40m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();
        updated!.BrlValue.Should().Be(320.50m);
        updated.Description.Should().Be("Medical appointment");
    }

    [Fact]
    public async Task UpdateEntryValues_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/controle-mae/entries/{Guid.NewGuid()}/values", new MaeLedgerEntryValuesUpdateDTO
        {
            BrlValue = 10m,
            GbpValue = 1m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntry_ExistingId_RemovesEntry()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "School supplies",
            SourceCurrency = "BRL",
            SourceValue = 350m
        });
        var entry = await created.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/controle-mae/entries/{entry!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await (await Client.GetAsync("/api/v1/financial/controle-mae/entries/from/2020-01-01"))
            .Content.ReadFromJsonAsync<List<MaeLedgerEntryDTO>>();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntry_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/controle-mae/entries/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEntryValues_OppositeSigns_Returns400WithMessage()
    {
        var entry = await CreateBrlEntry();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/controle-mae/entries/{entry.Id}/values", new MaeLedgerEntryValuesUpdateDTO
        {
            BrlValue = 100m,
            GbpValue = -15m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("BRL and GBP values must both be positive or both be negative.");
    }

    [Fact]
    public async Task UpdateEntryValues_MissingPaidCurrencyValue_Returns400WithMessage()
    {
        var entry = await CreateBrlEntry();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/controle-mae/entries/{entry.Id}/values", new MaeLedgerEntryValuesUpdateDTO
        {
            GbpValue = 15m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("BRL value is required and must not be zero.");
    }

    private async Task<MaeLedgerEntryDTO> CreateBrlEntry()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "School supplies",
            SourceCurrency = "BRL",
            SourceValue = 100m
        });
        return (await created.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>())!;
    }
}