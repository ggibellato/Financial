using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

public class AllocationBreakdownAcceptanceTests : ApiEndpointTests
{
    private const string AllocationBreakdownRoute = "/api/v1/financial/allocation-breakdown";
    private const string SeededBroker = "XPI";
    private const string SeededPortfolio = "Default";
    private const string UnclassifiedAsset = "BCIA11";
    private const string EquityAsset = "EQUITYUK";
    private const string BondAsset = "BONDUS";
    private const string RealEstateAsset = "REITBR";
    private const string UnpricedAsset = "NOPRICE";

    private static readonly DateTimeOffset Today = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PriceDate = DateOnly.FromDateTime(Today.UtcDateTime);
    private static readonly DateTime PurchaseDate = new(2025, 1, 1);

    public AllocationBreakdownAcceptanceTests()
        : base(timeProvider: new FakeTimeProvider(Today))
    {
    }

    [Fact]
    [Trait("AC", "P52-F02-allocation-breakdown-01")]
    public async Task EveryDimensionsPercentages_SumToExactlyOneHundred()
    {
        await SeedPricedPortfolioAsync();

        var breakdown = await GetBreakdownAsync();

        using var _ = new AssertionScope();
        breakdown.ByClass.Should().HaveCountGreaterThanOrEqualTo(3);
        breakdown.ByClass.Sum(entry => entry.Percentage).Should().Be(100m);
        breakdown.ByCurrency.Sum(entry => entry.Percentage).Should().Be(100m);
        breakdown.ByCountry.Sum(entry => entry.Percentage).Should().Be(100m);
        breakdown.ByBroker.Sum(entry => entry.Percentage).Should().Be(100m);
        breakdown.ByClass.Sum(entry => entry.MarketValue).Should().Be(breakdown.ByCountry.Sum(entry => entry.MarketValue));
    }

    [Fact]
    [Trait("AC", "P52-F02-allocation-breakdown-02")]
    public async Task AnUnpricedHolding_ChangesNoDimensionAndNeverAppearsAsAZeroValueSlice()
    {
        await SeedPricedPortfolioAsync();
        var before = await GetBreakdownAsync();

        await CreateAssetAsync(UnpricedAsset, CountryCode.BR, GlobalAssetClass.Cryptocurrency);
        await BuyAsync(UnpricedAsset, quantity: 10m, unitPrice: 7m);
        var after = await GetBreakdownAsync();

        using var _ = new AssertionScope();
        after.ByClass.Should().NotContain(entry => entry.Class == GlobalAssetClass.Cryptocurrency);
        after.ByClass.Should().NotContain(entry => entry.MarketValue == 0m);
        after.ByCountry.Should().NotContain(entry => entry.MarketValue == 0m);
        after.ByClass.Should().BeEquivalentTo(before.ByClass, "an unpriced holding leaves every other slice untouched");
        after.ByCountry.Should().BeEquivalentTo(before.ByCountry);
        after.ByCurrency.Should().BeEquivalentTo(before.ByCurrency);
        after.ByBroker.Should().BeEquivalentTo(before.ByBroker);
        after.ByClass.Sum(entry => entry.Percentage).Should().Be(100m);
    }

    [Fact]
    [Trait("AC", "P52-F02-allocation-breakdown-03")]
    public async Task AnUnclassifiedHolding_AppearsAsAnUnknownSliceInClassAndCountry()
    {
        await SeedPricedPortfolioAsync();

        var breakdown = await GetBreakdownAsync();

        using var _ = new AssertionScope();
        breakdown.ByClass.Should().ContainSingle(entry => entry.Class == GlobalAssetClass.Unknown && entry.MarketValue > 0m);
        breakdown.ByCountry.Should().ContainSingle(entry => entry.Country == CountryCode.Unknown && entry.MarketValue > 0m);
    }

    private async Task<AllocationBreakdownDTO> GetBreakdownAsync() =>
        (await Client.GetFromJsonAsync<AllocationBreakdownDTO>(AllocationBreakdownRoute))!;

    private async Task SeedPricedPortfolioAsync()
    {
        await SetPriceAsync(UnclassifiedAsset, 12.5m);

        await CreateAssetAsync(EquityAsset, CountryCode.UK, GlobalAssetClass.Equity);
        await BuyAsync(EquityAsset, quantity: 10m, unitPrice: 10m);
        await SetPriceAsync(EquityAsset, 40m);

        await CreateAssetAsync(BondAsset, CountryCode.US, GlobalAssetClass.Bond);
        await BuyAsync(BondAsset, quantity: 10m, unitPrice: 5m);
        await SetPriceAsync(BondAsset, 30m);

        await CreateAssetAsync(RealEstateAsset, CountryCode.BR, GlobalAssetClass.RealEstate);
        await BuyAsync(RealEstateAsset, quantity: 10m, unitPrice: 2m);
        await SetPriceAsync(RealEstateAsset, 20m);
    }

    private async Task CreateAssetAsync(string assetName, CountryCode country, GlobalAssetClass assetClass)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            Name = assetName,
            Exchange = "BVMF",
            Ticker = assetName,
            Country = country,
            Class = assetClass
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task BuyAsync(string assetName, decimal quantity, decimal unitPrice)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = assetName,
            Date = PurchaseDate,
            Type = nameof(Transaction.TransactionType.Buy),
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = 0m,
            Withheld = 0m
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetPriceAsync(string assetName, decimal price)
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = assetName,
            Date = PriceDate,
            Price = price
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
