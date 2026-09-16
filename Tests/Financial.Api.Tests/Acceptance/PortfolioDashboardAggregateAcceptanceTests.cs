using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

public class PortfolioDashboardAggregateAcceptanceTests : ApiEndpointTests
{
    private const string DashboardRoute = "/api/v1/financial/dashboard";
    private const string SeededBroker = "XPI";
    private const string SeededPortfolio = "Default";
    private const string SeededAsset = "BCIA11";
    private const string NewBroker = "T212";
    private const string NewPortfolio = "Growth";
    private const string NewAsset = "ACME";

    private static readonly DateTimeOffset Today = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PriceDate = DateOnly.FromDateTime(Today.UtcDateTime);

    public PortfolioDashboardAggregateAcceptanceTests()
        : base(timeProvider: new FakeTimeProvider(Today))
    {
    }

    [Fact]
    [Trait("AC", "P52-F01-dashboard-aggregate-01")]
    public async Task DashboardMoneyFigures_ReconcileWithTheSumOfEachBrokersOwnSummary()
    {
        await SeedSecondBrokerAsync();
        await SetPriceAsync(SeededBroker, SeededPortfolio, SeededAsset, 130m);

        var brokers = await Client.GetFromJsonAsync<List<BrokerDTO>>("/api/v1/financial/brokers");
        decimal marketValue = 0m, invested = 0m, grossCredits = 0m;

        foreach (var broker in brokers!)
        {
            var scope = broker.Status == "Active" ? "active" : "historic";
            var summary = await Client.GetFromJsonAsync<AggregatedSummaryDTO>(
                $"/api/v1/financial/summary/broker/{broker.Name}?scope={scope}");

            grossCredits += summary!.TotalCredits;
            if (broker.Status != "Active")
            {
                continue;
            }

            marketValue += summary.MarketValue ?? 0m;
            invested += summary.TotalInvested;
        }

        var dashboard = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        using var _ = new AssertionScope();
        dashboard!.IsPartial.Should().BeFalse("every Active holding is priced in this fixture");
        dashboard.MarketValue.Should().Be(marketValue);
        dashboard.Invested.Should().Be(invested);
        dashboard.UnrealisedGainLoss.Should().Be(marketValue - invested);
        dashboard.IncomeLifetime.Should().Be(grossCredits, "nothing in this fixture withholds, so net income equals the gross credits each broker reports");
        dashboard.GrossXirr.Should().NotBeNull();
        dashboard.NetXirr.Should().NotBeNull();
    }

    [Fact]
    [Trait("AC", "P52-F01-dashboard-aggregate-01")]
    public async Task PortfolioXirr_MovesWhenAnyOneBrokersCashFlowsChange()
    {
        await SeedSecondBrokerAsync();
        await SetPriceAsync(SeededBroker, SeededPortfolio, SeededAsset, 130m);
        var before = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        await AddTransactionAsync(NewBroker, NewPortfolio, NewAsset, new DateTime(2026, 1, 15), "Buy", 40m, 45m);
        var after = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        using var _ = new AssertionScope();
        before!.GrossXirr.Should().NotBeNull();
        after!.GrossXirr.Should().NotBeNull();
        after.GrossXirr.Should().NotBe(before.GrossXirr, "the combined series spans every broker, not just the first");
    }

    [Fact]
    [Trait("AC", "P52-F01-dashboard-aggregate-02")]
    public async Task IsPartial_IsTrueWhileAnyActiveHoldingHasNoMarketValue()
    {
        await SeedSecondBrokerAsync();

        var withUnpricedHolding = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        await SetPriceAsync(SeededBroker, SeededPortfolio, SeededAsset, 130m);
        var fullyPriced = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        using var _ = new AssertionScope();
        withUnpricedHolding!.IsPartial.Should().BeTrue();
        withUnpricedHolding.UnvaluedHoldingCount.Should().Be(1);
        withUnpricedHolding.MarketValue.Should().BeGreaterThan(0m, "the priced holding still contributes its own market value");
        fullyPriced!.IsPartial.Should().BeFalse();
        fullyPriced.UnvaluedHoldingCount.Should().Be(0);
    }

    [Fact]
    [Trait("AC", "P52-F01-dashboard-aggregate-03")]
    public async Task RealisedGainLoss_CountsOnlyTheActiveDisposalRecord_NeverTheSupersededOne()
    {
        await SeedSecondBrokerAsync();
        var baseline = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        await AddTransactionAsync(NewBroker, NewPortfolio, NewAsset, new DateTime(2025, 6, 1), "Sell", 4m, 15m);
        var beforeSupersede = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        await AddTransactionAsync(NewBroker, NewPortfolio, NewAsset, new DateTime(2025, 3, 1), "Buy", 10m, 20m);
        var afterSupersede = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        var asset = await Client.GetFromJsonAsync<AssetDetailsDTO>(
            $"/api/v1/financial/assets/{NewBroker}/{NewPortfolio}/{NewAsset}");
        var activeRecords = asset!.DisposalRecords.Where(record => record.Status == DisposalRecordStatus.Active).ToList();
        var supersededGainLoss = asset.DisposalRecords
            .Where(record => record.Status == DisposalRecordStatus.Superseded)
            .Sum(record => record.GainLoss);

        using var _ = new AssertionScope();
        activeRecords.Should().ContainSingle();
        supersededGainLoss.Should().Be(20m, "the original disposal was superseded, not rewritten");
        (beforeSupersede!.RealisedGainLoss - baseline!.RealisedGainLoss).Should().Be(20m);
        (afterSupersede!.RealisedGainLoss - baseline.RealisedGainLoss).Should().Be(
            activeRecords[0].GainLoss, "only the surviving Active record counts");
        (afterSupersede.RealisedGainLoss - baseline.RealisedGainLoss).Should().NotBe(
            activeRecords[0].GainLoss + supersededGainLoss, "the superseded record's own gain is not added on top");
    }

    [Fact]
    [Trait("AC", "P52-F01-dashboard-aggregate-04")]
    public async Task IncomeYtd_ExcludesAPriorCalendarYearsCredit_WhileLifetimeIncomeKeepsIt()
    {
        await SeedSecondBrokerAsync();
        var before = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        await AddCreditAsync(new DateTime(2025, 11, 1), value: 100m, withheld: 0m);
        await AddCreditAsync(new DateTime(2026, 3, 1), value: 50m, withheld: 10m);
        var after = await Client.GetFromJsonAsync<PortfolioDashboardDTO>(DashboardRoute);

        using var _ = new AssertionScope();
        (after!.IncomeYtd - before!.IncomeYtd).Should().Be(40m, "only the current-year credit counts, net of what was withheld");
        (after.IncomeLifetime - before.IncomeLifetime).Should().Be(140m, "lifetime income never applies a date floor");
    }

    private async Task SeedSecondBrokerAsync()
    {
        var broker = await Client.PostAsJsonAsync("/api/v1/financial/brokers", new BrokerCreateDTO
        {
            Name = NewBroker,
            Currency = "BRL"
        });
        broker.StatusCode.Should().Be(HttpStatusCode.OK);

        var portfolio = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO
        {
            BrokerName = NewBroker,
            Name = NewPortfolio
        });
        portfolio.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = NewBroker,
            PortfolioName = NewPortfolio,
            Name = NewAsset,
            ISIN = "US0378331005",
            Exchange = "BVMF",
            Ticker = NewAsset
        });
        asset.StatusCode.Should().Be(HttpStatusCode.OK);

        await AddTransactionAsync(NewBroker, NewPortfolio, NewAsset, new DateTime(2025, 1, 1), "Buy", 10m, 10m);
        await SetPriceAsync(NewBroker, NewPortfolio, NewAsset, 18m);
    }

    private async Task AddTransactionAsync(
        string brokerName, string portfolioName, string assetName, DateTime date, string type, decimal quantity, decimal unitPrice)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = brokerName,
            PortfolioName = portfolioName,
            AssetName = assetName,
            Date = date,
            Type = type,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = 0m,
            Withheld = 0m
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AddCreditAsync(DateTime date, decimal value, decimal withheld)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = NewBroker,
            PortfolioName = NewPortfolio,
            AssetName = NewAsset,
            Date = date,
            Type = nameof(Credit.CreditType.Dividend),
            Value = value,
            Withheld = withheld
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetPriceAsync(string brokerName, string portfolioName, string assetName, decimal price)
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = brokerName,
            PortfolioName = portfolioName,
            AssetName = assetName,
            Date = PriceDate,
            Price = price
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
