using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Api.Tests.Acceptance;

public class ReportingCurrencySettingAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";

    public ReportingCurrencySettingAcceptanceTests()
        : base(exchangeRateProvider: new StubExchangeRateProvider(0.15m))
    {
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-01")]
    public async Task GetReportingCurrency_DefaultsToGbp()
    {
        var response = await Client.GetAsync("/api/v1/financial/reporting-currency");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ReportingCurrencySettingDTO>();
        dto!.Currency.Should().Be("GBP");
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-02")]
    public async Task GetBrokerSummary_IncludesConvertedFieldsAlongsideEveryNativeField()
    {
        var response = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto!.TotalBought.Should().Be(1000m);
        dto.ReportingCurrency.Should().Be("GBP");
        dto.ConvertedInvested.Should().Be(117m);
        dto.IsPartial.Should().BeFalse();
        dto.IsReportingCurrencyUnavailable.Should().BeFalse();
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-03")]
    public async Task ChangingReportingCurrency_ChangesTheConvertedFiguresOnTheVeryNextRead()
    {
        var beforeResponse = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        var before = await beforeResponse.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        before!.ReportingCurrency.Should().Be("GBP");
        before.ConvertedInvested.Should().Be(117m);

        var putResponse = await Client.PutAsJsonAsync("/api/v1/financial/reporting-currency", new ReportingCurrencySettingDTO { Currency = "BRL" });
        putResponse.EnsureSuccessStatusCode();

        var afterResponse = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        var after = await afterResponse.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        after!.ReportingCurrency.Should().Be("BRL");
        after.ConvertedInvested.Should().Be(780m, "XPI is already BRL, so no rate applies once the reporting currency matches it");
    }
}

public class ReportingCurrencyConvertedTotalsAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";
    private static readonly DateOnly BuyDate = new(2024, 1, 1);
    private static readonly DateOnly SellDate = new(2024, 6, 1);
    private static readonly DateTimeOffset AsOf = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    public ReportingCurrencyConvertedTotalsAcceptanceTests()
        : base(
            exchangeRateProvider: new FakeExchangeRateProvider((date, _, _) => date switch
            {
                _ when date == BuyDate => 0.10m,
                _ when date == SellDate => 0.20m,
                _ => 0.50m,
            }),
            timeProvider: new FakeTimeProvider(AsOf))
    {
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-04")]
    public async Task ConvertedInvested_IsTheSumOfEachContributingTransactionConvertedAtItsOwnDate()
    {
        var response = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto!.ConvertedInvested.Should().Be(56m, "1000 bought at 0.10 (56) minus 220 sold at 0.20 (44) = 56, not a single spot rate applied to the native total");
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-05")]
    public async Task ConvertedMarketValue_UsesTodaysRate_NotAnyContributingRecordsOwnDate()
    {
        var priceResponse = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = BrokerName,
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = BuyDate,
            Price = 100m,
        });
        priceResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto!.MarketValue.Should().Be(800m, "quantity 8 held * price 100");
        dto.ConvertedMarketValue.Should().Be(400m, "800 at today's 0.50 rate, not the Buy/Sell dates' 0.10/0.20");
    }
}

public class ReportingCurrencyPartialConversionAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";
    private static readonly DateOnly BuyDate = new(2024, 1, 1);

    public ReportingCurrencyPartialConversionAcceptanceTests()
        : base(exchangeRateProvider: new FakeExchangeRateProvider((date, _, _) => date == BuyDate ? 0.10m : null))
    {
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-06")]
    public async Task SomeContributingRecordsCannotConvert_ResponseIsFlaggedPartial_AndReturnsWhatDidConvert()
    {
        var response = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto!.IsPartial.Should().BeTrue();
        dto.IsReportingCurrencyUnavailable.Should().BeFalse();
        dto.ConvertedInvested.Should().Be(100m, "only the Buy leg (1000 at 0.10) converted; the Sell leg's date has no rate");
    }
}

public class ReportingCurrencyUnavailableAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";

    public ReportingCurrencyUnavailableAcceptanceTests()
        : base(exchangeRateProvider: new StubExchangeRateProvider(null))
    {
    }

    [Fact]
    [Trait("AC", "P49-F03-reporting-currency-setting-and-converted-totals-07")]
    public async Task ExchangeRateProviderUnreachableForTheWholeRequest_FlagsUnavailable_NativeFieldsStillCorrect()
    {
        var response = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto!.IsReportingCurrencyUnavailable.Should().BeTrue();
        dto.ConvertedInvested.Should().BeNull();
        dto.ConvertedMarketValue.Should().BeNull();
        dto.TotalBought.Should().Be(1000m);
        dto.TotalSold.Should().Be(220m);
    }
}
