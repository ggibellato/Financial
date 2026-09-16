using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

public class DataQualityWarningsAcceptanceTests : ApiEndpointTests
{
    private const string ReportRoute = "/api/v1/financial/data-quality-report";
    private const string SeededBroker = "XPI";
    private const string SeededPortfolio = "Default";
    private const string SeededAsset = "BCIA11";
    private const string ProviderValuedAsset = "PROVIDERVALUED";

    private static readonly DateTimeOffset Today = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly TodayDate = DateOnly.FromDateTime(Today.UtcDateTime);

    public DataQualityWarningsAcceptanceTests()
        : base(timeProvider: new FakeTimeProvider(Today))
    {
    }

    [Fact]
    [Trait("AC", "P52-F03-data-quality-warnings-01")]
    public async Task MissingPriceCategory_NamesExactlyTheActiveHoldingsWithNoRecordedPrice()
    {
        var beforePricing = await GetReportAsync();

        await SetPriceAsync(SeededAsset, TodayDate, 130m);
        var afterPricing = await GetReportAsync();

        using var _ = new AssertionScope();
        beforePricing.UnpricedOpenHoldings.Should().ContainSingle(f => f.AssetName == SeededAsset);
        afterPricing.UnpricedOpenHoldings.Should().BeEmpty();
    }

    [Fact]
    [Trait("AC", "P52-F03-data-quality-warnings-01")]
    public async Task MissingCostBasisCategory_NamesExactlyTheOpenHoldingsWithNoDerivableCostBasis()
    {
        await CreateProviderValuedAssetAsync();
        var beforePricing = await GetReportAsync();

        await SetPriceAsync(ProviderValuedAsset, TodayDate, 1500m);
        var afterPricing = await GetReportAsync();

        using var _ = new AssertionScope();
        beforePricing.OpenHoldingsMissingCostBasis.Should().BeEmpty();
        afterPricing.OpenHoldingsMissingCostBasis.Should().ContainSingle(f => f.AssetName == ProviderValuedAsset);
        afterPricing.UnpricedOpenHoldings.Should().NotContain(f => f.AssetName == ProviderValuedAsset);
    }

    [Fact]
    [Trait("AC", "P52-F03-data-quality-warnings-01")]
    public async Task UnresolvedTaxClassificationCategory_NamesEveryClassificationWithNoApplicableRule()
    {
        var before = await GetReportAsync();

        await AddCreditAsync(new DateTime(2026, 3, 1), 50m);
        var withUnruledCredit = await GetReportAsync();

        await CreateDividendTaxRuleAsync();
        await AddCreditAsync(new DateTime(2026, 6, 1), 70m);
        var withRuledCredit = await GetReportAsync();

        using var _ = new AssertionScope();
        before.UnresolvedTaxClassifications.Should().NotContain(f => f.TaxYear == "2026");
        withUnruledCredit.UnresolvedTaxClassifications.Should().ContainSingle(
            f => f.AssetName == SeededAsset && f.TaxYear == "2026" && f.EventCategory == EventCategory.Dividend);
        withUnruledCredit.UnresolvedTaxClassifications.Should().HaveCount(before.UnresolvedTaxClassifications.Count + 1);
        withRuledCredit.UnresolvedTaxClassifications.Should().HaveCount(
            withUnruledCredit.UnresolvedTaxClassifications.Count,
            "a credit an applicable tax rule already covers is classified Final, never an unresolved gap");
    }

    [Fact]
    [Trait("AC", "P52-F03-data-quality-warnings-01")]
    public async Task ImpossibleCashFlowSequenceCategory_StaysEmptyWhileEverySaleIsCovered()
    {
        var before = await GetReportAsync();

        var oversell = await PostTransactionAsync(SeededAsset, new DateTime(2026, 7, 1), "Sell", quantity: 999m, unitPrice: 110m);
        var after = await GetReportAsync();

        using var _ = new AssertionScope();
        before.SalesExceedPurchases.Should().BeEmpty();
        oversell.StatusCode.Should().NotBe(HttpStatusCode.OK, "the API refuses to record a sale the holding cannot cover");
        after.SalesExceedPurchases.Should().BeEmpty();
    }

    [Fact]
    [Trait("AC", "P52-F03-data-quality-warnings-04")]
    public async Task StaleValuationCount_MatchesTheActiveHoldingsWhoseMarketStatusIsStale()
    {
        await SetPriceAsync(SeededAsset, TodayDate.AddDays(-10), 130m);
        var stalePrice = await GetReportAsync();

        await SetPriceAsync(SeededAsset, TodayDate, 131m);
        var currentPrice = await GetReportAsync();

        using var _ = new AssertionScope();
        stalePrice.StaleValuationCount.Should().Be(1);
        currentPrice.StaleValuationCount.Should().Be(0);
    }

    [Fact]
    public async Task EveryDashboardCategoryStaysInThePayloadWhenItHasNoFindings()
    {
        await SetPriceAsync(SeededAsset, TodayDate, 130m);

        var payload = await Client.GetStringAsync(ReportRoute);
        var report = await GetReportAsync();

        using var _ = new AssertionScope();
        report.SalesExceedPurchases.Should().BeEmpty();
        report.UnpricedOpenHoldings.Should().BeEmpty();
        report.OpenHoldingsMissingCostBasis.Should().BeEmpty();
        report.StaleValuationCount.Should().Be(0);
        payload.Should().Contain("\"salesExceedPurchases\":[]");
        payload.Should().Contain("\"unpricedOpenHoldings\":[]");
        payload.Should().Contain("\"openHoldingsMissingCostBasis\":[]");
        payload.Should().Contain("\"staleValuationCount\":0");
        payload.Should().Contain("\"unresolvedTaxClassifications\":");
    }

    private async Task<DataQualityReportDTO> GetReportAsync() =>
        (await Client.GetFromJsonAsync<DataQualityReportDTO>(ReportRoute))!;

    private async Task CreateProviderValuedAssetAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            Name = ProviderValuedAsset,
            ISIN = "US0378331005",
            Exchange = "BVMF",
            Ticker = ProviderValuedAsset,
            ValuationMethod = ValuationMethod.ProviderValue
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetPriceAsync(string assetName, DateOnly date, decimal price)
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = assetName,
            Date = date,
            Price = price
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task CreateDividendTaxRuleAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/tax-rules", new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "BR dividend rule",
            Description = "Covers dividends from 2026",
            EffectiveFrom = new DateOnly(2026, 1, 1)
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AddCreditAsync(DateTime date, decimal value)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = SeededAsset,
            Date = date,
            Type = nameof(Credit.CreditType.Dividend),
            Value = value,
            Withheld = 0m
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> PostTransactionAsync(
        string assetName, DateTime date, string type, decimal quantity, decimal unitPrice) =>
        Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = assetName,
            Date = date,
            Type = type,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = 0m,
            Withheld = 0m
        });
}
