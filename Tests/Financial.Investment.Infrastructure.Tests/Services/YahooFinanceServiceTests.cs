using System.Net;
using System.Net.Http;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class YahooFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankTicker_ThrowsArgumentException()
    {
        var service = CreateService(_ => throw new InvalidOperationException("should not be called"));
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Ticker is required.*");
    }

    [Fact]
    public void GetAssetValue_MissingExchange_ThrowsArgumentException()
    {
        var service = CreateService(_ => throw new InvalidOperationException("should not be called"));
        var request = new AssetValueRequestDTO { Ticker = "BBAS3F" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("*requires an Exchange*");
    }

    [Fact]
    public void GetAssetValue_UnsupportedExchange_ThrowsInvalidOperationException_WithoutCallingHttp()
    {
        var service = CreateService(_ => throw new InvalidOperationException("should not be called"));
        var request = new AssetValueRequestDTO { Exchange = "TSX", Ticker = "SHOP" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*TSX*not supported*");
    }

    [Fact]
    public void GetAssetValue_SuccessfulResponse_ParsesSnapshot()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"chart":{"result":[{"meta":{"regularMarketPrice":21.18,"regularMarketTime":1751500000,"shortName":"BRASIL      ON      NM"}}]}}
                """)
        });
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        var result = service.GetAssetValue(request);

        result.Ticker.Should().Be("BBAS3F");
        result.Name.Should().Be("BRASIL      ON      NM");
        result.Price.Should().Be(21.18m);
        result.AsOf.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1751500000));
    }

    [Fact]
    public void GetAssetValue_SuccessfulResponseWithoutTimeOrName_FallsBackToTickerAndUtcNow()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"chart":{"result":[{"meta":{"regularMarketPrice":10.5}}]}}""")
        });
        var request = new AssetValueRequestDTO { Exchange = "NASDAQ", Ticker = "AAPL" };

        var result = service.GetAssetValue(request);

        result.Name.Should().Be("AAPL");
        result.AsOf.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetAssetValue_NonSuccessStatusCode_ThrowsInvalidOperationException()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*404*");
    }

    [Fact]
    public void GetAssetValue_MalformedBody_ThrowsInvalidOperationException()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json")
        });
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*unreadable*");
    }

    [Fact]
    public void GetAssetValue_MissingPriceField_ThrowsInvalidOperationException()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"chart":{"result":[]}}""")
        });
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*did not include a price*");
    }

    [Fact]
    public void GetAssetValue_TransportException_ThrowsInvalidOperationException()
    {
        var service = CreateService(_ => throw new HttpRequestException("network down"));
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*request failed*");
    }

    private static YahooFinanceService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new FakeHttpMessageHandler(responder);
        return new YahooFinanceService(new HttpClient(handler));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _responder(request);
    }
}
