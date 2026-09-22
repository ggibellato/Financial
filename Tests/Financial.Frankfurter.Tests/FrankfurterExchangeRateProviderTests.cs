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

    [Fact]
    public async Task FetchAsync_WithSuccessfulResponse_ParsesBothCurrencies()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-18","rates":{"BRL":5.452317,"GBP":0.771845}}""")
        });

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.BrlRate.Should().Be(5.452317m);
        result.GbpRate.Should().Be(0.771845m);
    }

    [Fact]
    public async Task FetchAsync_IssuesExactlyOneHttpCallForAResolvedDate()
    {
        var requestCount = 0;
        var provider = CreateProvider(request =>
        {
            requestCount++;
            var query = Uri.UnescapeDataString(request.RequestUri!.Query);
            query.Should().Contain("from=USD").And.Contain("to=BRL,GBP");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-18","rates":{"BRL":5.45,"GBP":0.77}}""")
            };
        });

        await provider.FetchAsync(new DateOnly(2026, 9, 18));

        requestCount.Should().Be(1);
    }

    [Fact]
    public async Task FetchAsync_WithOnlyBrlInResponse_ReturnsPartialResult()
    {
        var requestedDates = new List<string>();
        var provider = CreateProvider(request =>
        {
            requestedDates.Add(ExtractDate(request));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-18","rates":{"BRL":5.45}}""")
            };
        });

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.BrlRate.Should().Be(5.45m);
        result.GbpRate.Should().BeNull();
        requestedDates.Should().Equal("2026-09-18");
    }

    [Fact]
    public async Task FetchAsync_WithOnlyGbpInResponse_ReturnsPartialResult()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-18","rates":{"GBP":0.77}}""")
        });

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.GbpRate.Should().Be(0.77m);
        result.BrlRate.Should().BeNull();
    }

    [Fact]
    public async Task FetchAsync_WhenExactDateHasNoRates_FallsBackToNearestEarlierDateWithinTenDays()
    {
        const string AvailableDate = "2026-09-17";
        var requestedDates = new List<string>();
        var provider = CreateProvider(request =>
        {
            var date = ExtractDate(request);
            requestedDates.Add(date);

            if (date == AvailableDate)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-17","rates":{"BRL":5.44,"GBP":0.76}}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-17","rates":{}}""")
            };
        });

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 19));

        result.BrlRate.Should().Be(5.44m);
        result.GbpRate.Should().Be(0.76m);
        requestedDates.Should().Equal("2026-09-19", "2026-09-18", "2026-09-17");
    }

    [Fact]
    public async Task FetchAsync_WhenNoRateWithinTenDayWindow_ReturnsEmptyResult()
    {
        var requestedDates = new List<string>();
        var provider = CreateProvider(request =>
        {
            requestedDates.Add(ExtractDate(request));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"amount":1,"base":"USD","date":"2026-09-18","rates":{}}""")
            };
        });

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.BrlRate.Should().BeNull();
        result.GbpRate.Should().BeNull();
        requestedDates.Should().HaveCount(11);
    }

    [Fact]
    public async Task FetchAsync_WhenHttpRequestThrows_ReturnsEmptyResultAndLogsExceptionType()
    {
        var logger = new RecordingLogger<FrankfurterExchangeRateProvider>();
        var provider = new FrankfurterExchangeRateProvider(
            CreateClient(new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"))), logger);

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.BrlRate.Should().BeNull();
        result.GbpRate.Should().BeNull();
        logger.Entries.Should().NotBeEmpty();
        logger.Entries.Should().OnlyContain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains(nameof(HttpRequestException)));
    }

    [Fact]
    public async Task FetchAsync_WithMalformedBody_ReturnsEmptyResult()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json")
        });

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.BrlRate.Should().BeNull();
        result.GbpRate.Should().BeNull();
    }

    [Fact]
    public async Task FetchAsync_WithNonSuccessStatusCode_ReturnsEmptyResult()
    {
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await provider.FetchAsync(new DateOnly(2026, 9, 18));

        result.BrlRate.Should().BeNull();
        result.GbpRate.Should().BeNull();
    }

    private static string ExtractDate(HttpRequestMessage request) =>
        request.RequestUri!.AbsolutePath.TrimStart('/');

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
}
