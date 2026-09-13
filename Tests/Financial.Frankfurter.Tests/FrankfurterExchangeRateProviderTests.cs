using System.Net;
using Financial.Integrations.Frankfurter;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Frankfurter.Tests;

public class FrankfurterExchangeRateProviderTests
{
    private static FrankfurterExchangeRateProvider CreateProvider(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(CreateClient(new FakeHttpMessageHandler(respond)), NullLogger<FrankfurterExchangeRateProvider>.Instance);

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-01")]
    public async Task GetHistoricalRateAsync_WithSuccessfulResponse_ParsesTheRate()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{"USD":0.19}}""")
        });

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.USD);

        rate.Should().Be(0.19m);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WithNonSuccessStatusCode_ReturnsNull()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WithMalformedBody_ReturnsNull()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json")
        });

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WhenHttpRequestThrows_ReturnsNull()
    {
        var provider = CreateProvider(_ => throw new HttpRequestException("network down"));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WhenHttpRequestThrows_LogsTheExceptionType()
    {
        var logger = new RecordingLogger<FrankfurterExchangeRateProvider>();
        var provider = new FrankfurterExchangeRateProvider(
            CreateClient(new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"))), logger);

        await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        logger.Entries.Should().NotBeEmpty();
        logger.Entries.Should().OnlyContain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains(nameof(HttpRequestException)));
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WhenResponseMissingRequestedCurrency_ReturnsNull()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{"EUR":0.15}}""")
        });

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WithExactDateRate_DoesNotFallBack()
    {
        var requestedDates = new List<string>();
        var provider = CreateProvider(request =>
        {
            requestedDates.Add(ExtractDate(request));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{"GBP":0.146}}""")
            };
        });

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().Be(0.146m);
        requestedDates.Should().Equal("2026-07-01");
    }

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-05")]
    public async Task GetHistoricalRateAsync_WhenExactDateHasNoRate_FallsBackToNearestEarlierDateWithinTenDays()
    {
        const string AvailableDate = "2026-07-02";
        var requestedDates = new List<string>();
        var provider = CreateProvider(request =>
        {
            var date = ExtractDate(request);
            requestedDates.Add(date);

            if (date == AvailableDate)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-02","rates":{"GBP":0.15}}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-02","rates":{}}""")
            };
        });

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 4), Currency.BRL, Currency.GBP);

        rate.Should().Be(0.15m);
        requestedDates.Should().Equal("2026-07-04", "2026-07-03", "2026-07-02");
    }

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-06")]
    public async Task GetHistoricalRateAsync_WhenNoRateWithinTenDayWindow_ReturnsNull()
    {
        var requestedDates = new List<string>();
        var provider = CreateProvider(request =>
        {
            requestedDates.Add(ExtractDate(request));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{}}""")
            };
        });

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 15), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
        requestedDates.Should().HaveCount(11);
        requestedDates.Should().Equal(
            "2026-07-15", "2026-07-14", "2026-07-13", "2026-07-12", "2026-07-11",
            "2026-07-10", "2026-07-09", "2026-07-08", "2026-07-07", "2026-07-06", "2026-07-05");
    }

    private static string ExtractDate(HttpRequestMessage request) =>
        request.RequestUri!.AbsolutePath.TrimStart('/');

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
}
