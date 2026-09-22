using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

public class FxRateProviderCompositionAcceptanceTests : ApiEndpointTests
{
    private static readonly DateOnly HistoricalDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-30);

    [Fact]
    public void ExchangeRateProvider_ResolvedTwice_IsTheSameSharedSingleton()
    {
        var first = Services.GetRequiredService<IExchangeRateProvider>();
        var second = Services.GetRequiredService<IExchangeRateProvider>();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public async Task ControleMaeCreateEntry_UsesRateFromTheLayeredFxRateProviderChain()
    {
        var store = Services.GetRequiredService<IFxRateStore>();
        await store.SetRateAsync(HistoricalDate, new FxRateRecord(5.0m, 0.8m, "frankfurter", DateTimeOffset.UtcNow));

        var response = await Client.PostAsJsonAsync("/api/v1/financial/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = HistoricalDate,
            Description = "FX composition check",
            Note = "P54 F04",
            SourceCurrency = "BRL",
            SourceValue = 350m
        });

        response.EnsureSuccessStatusCode();
        var entry = await response.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();

        entry.Should().NotBeNull();
        entry!.BrlValue.Should().Be(350m);
        entry.GbpValue.Should().Be(56m, "0.8/5.0 * 350 - the rate must come from the seeded store, not a live Frankfurter call");
    }
}
