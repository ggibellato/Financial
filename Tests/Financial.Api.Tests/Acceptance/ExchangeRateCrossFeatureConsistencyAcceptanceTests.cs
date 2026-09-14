using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

/// <summary>
/// Proves the P49 "Cross-Feature Integration" PRD bullet that F01's shared IExchangeRateProvider
/// produces the one rate that both F02 (entry-time FxRateSnapshot capture) and F03 (on-demand
/// converted-totals) use for the same date/currency pair - a date-keyed fake rate function, rather
/// than one flat constant, is what makes this a real proof rather than two codepaths coincidentally
/// agreeing on the only value either could ever return.
/// </summary>
public class ExchangeRateCrossFeatureConsistencyAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "FxParityXPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "TESTA";
    private static readonly DateTime TransactionDate = new(2026, 7, 1);

    public ExchangeRateCrossFeatureConsistencyAcceptanceTests()
        : base(exchangeRateProvider: new FakeExchangeRateProvider(
            (date, _, _) => date == DateOnly.FromDateTime(TransactionDate) ? 0.12m : null))
    {
    }

    public override async Task InitializeAsync()
    {
        var repository = Services.GetRequiredService<IInvestmentRepository>();
        await repository.ApplyAndSaveAsync(() =>
        {
            var broker = Broker.Create(BrokerName, "BRL");
            var portfolio = broker.AddPortfolio(PortfolioName);
            portfolio.AddAsset(Asset.Create(AssetName, "TESTISIN", "BVMF", AssetName));
            repository.GetInvestments().AddActiveBroker(broker);
            return true;
        });
    }

    [Fact]
    public async Task RateCapturedAtEntryByF02_MatchesTheRateF03UsesForTheSameDateOnDemand()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = TransactionDate,
            Type = "Buy",
            Quantity = 100m,
            UnitPrice = 10m,
            Fees = 0m
        });
        response.EnsureSuccessStatusCode();

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var created = repository.GetAsset(BrokerName, PortfolioName, AssetName)!.Transactions.Should().ContainSingle().Subject;
        created.FxRateSnapshot.Should().NotBeNull();
        created.FxRateSnapshot!.Rate.Should().Be(0.12m, "F02's entry-time capture must use F01's rate for this transaction's own date");

        var summaryResponse = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();

        summary!.TotalBought.Should().Be(1000m, "100 units at 10 = 1000 native BRL");
        summary.ConvertedInvested.Should().Be(
            120m, "F03's on-demand conversion must use the exact same 0.12 rate F02 captured for this transaction's own date");
    }
}
