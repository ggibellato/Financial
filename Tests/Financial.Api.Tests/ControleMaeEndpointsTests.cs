using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ControleMaeEndpointsTests
{
    [Fact]
    public async Task CreateEntry_ValidRequest_ReturnsOkWithBothCurrenciesPopulated()
    {
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(0.146m));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
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
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(null));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
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
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(1.5m));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
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
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(1.5m));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            Description = "Future",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("future");
    }

    [Fact]
    public async Task GetEntriesByMonth_ReturnsOnlyEntriesForThatMonth()
    {
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(1.5m));
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "July entry",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });
        await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 8, 10),
            Description = "August entry",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });

        var response = await client.GetAsync("/api/v1/financial/controle-mae/entries/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<MaeLedgerEntryDTO>>();
        entries.Should().ContainSingle(e => e.Description == "July entry");
    }

    [Fact]
    public async Task UpdateEntryValues_ExistingId_ReturnsOkAndUpdatesOnlyValues()
    {
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(null));
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new CreateMaeLedgerEntryDTO
        {
            Date = new DateOnly(2026, 7, 16),
            Description = "Medical appointment",
            SourceCurrency = "GBP",
            SourceValue = 40m
        });
        var createdEntry = await created.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/controle-mae/entries/{createdEntry!.Id}/values", new UpdateMaeLedgerEntryValuesDTO
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
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(1.5m));
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/controle-mae/entries/{Guid.NewGuid()}/values", new UpdateMaeLedgerEntryValuesDTO
        {
            BrlValue = 10m,
            GbpValue = 1m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        private readonly decimal? _rate;

        public StubExchangeRateProvider(decimal? rate)
        {
            _rate = rate;
        }

        public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to) =>
            Task.FromResult(_rate);
    }
}
