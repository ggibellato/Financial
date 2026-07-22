using System.Net;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Services;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Services;

public class FrankfurterExchangeRateProviderTests
{
    [Fact]
    public async Task GetHistoricalRateAsync_WithSuccessfulResponse_ParsesTheRate()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{"GBP":0.146}}""")
        });
        var provider = new FrankfurterExchangeRateProvider(CreateClient(handler));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().Be(0.146m);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WithNonSuccessStatusCode_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var provider = new FrankfurterExchangeRateProvider(CreateClient(handler));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WithMalformedBody_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json")
        });
        var provider = new FrankfurterExchangeRateProvider(CreateClient(handler));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WhenHttpRequestThrows_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"));
        var provider = new FrankfurterExchangeRateProvider(CreateClient(handler));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_WhenResponseMissingRequestedCurrency_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{"EUR":0.15}}""")
        });
        var provider = new FrankfurterExchangeRateProvider(CreateClient(handler));

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

        rate.Should().BeNull();
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
