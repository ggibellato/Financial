using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class AssetPriceServiceTests
{
    [Fact]
    public void GetCurrentPrice_NullRequest_ThrowsArgumentNullException()
    {
        var service = new AssetPriceService([]);

        Action act = () => service.GetCurrentPrice(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCurrentPrice_BlankTicker_ThrowsArgumentException()
    {
        var service = new AssetPriceService([]);
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetCurrentPrice_NoFetchersRegistered_ThrowsInvalidOperationException()
    {
        var service = new AssetPriceService([]);
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "BCIA11" };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*No asset price fetcher is registered*");
    }

    [Fact]
    public void GetCurrentPrice_CryptocurrencyAssetClass_DispatchesToMatchingFetcher()
    {
        var cryptoSnapshot = new AssetValueSnapshot("BTC", "Bitcoin", 50000m, DateTimeOffset.UtcNow);
        var standardSnapshot = new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, DateTimeOffset.UtcNow);
        var standardFetcher = new StubFetcher(assetClass => assetClass != GlobalAssetClass.Cryptocurrency, standardSnapshot);
        var cryptoFetcher = new StubFetcher(assetClass => assetClass == GlobalAssetClass.Cryptocurrency, cryptoSnapshot);
        var service = new AssetPriceService([standardFetcher, cryptoFetcher]);
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BTC", AssetClass = GlobalAssetClass.Cryptocurrency };

        var result = service.GetCurrentPrice(request);

        result.Name.Should().Be("Bitcoin");
        result.Price.Should().Be(50000m);
    }

    [Fact]
    public void GetCurrentPrice_NonCryptocurrencyAssetClass_DispatchesToMatchingFetcher()
    {
        var cryptoSnapshot = new AssetValueSnapshot("BTC", "Bitcoin", 50000m, DateTimeOffset.UtcNow);
        var standardSnapshot = new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, DateTimeOffset.UtcNow);
        var standardFetcher = new StubFetcher(assetClass => assetClass != GlobalAssetClass.Cryptocurrency, standardSnapshot);
        var cryptoFetcher = new StubFetcher(assetClass => assetClass == GlobalAssetClass.Cryptocurrency, cryptoSnapshot);
        var service = new AssetPriceService([standardFetcher, cryptoFetcher]);
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "BCIA11", AssetClass = GlobalAssetClass.Equity };

        var result = service.GetCurrentPrice(request);

        result.Name.Should().Be("Some ETF");
        result.Price.Should().Be(10.5m);
    }

    [Fact]
    public void GetCurrentPrice_NoFetcherSupportsAssetClass_FallsBackToFirstRegisteredFetcher()
    {
        var firstSnapshot = new AssetValueSnapshot("XXX", "First Fetcher", 1m, DateTimeOffset.UtcNow);
        var secondSnapshot = new AssetValueSnapshot("YYY", "Second Fetcher", 2m, DateTimeOffset.UtcNow);
        var firstFetcher = new StubFetcher(_ => false, firstSnapshot);
        var secondFetcher = new StubFetcher(_ => false, secondSnapshot);
        var service = new AssetPriceService([firstFetcher, secondFetcher]);
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "XXX", AssetClass = GlobalAssetClass.Bond };

        var result = service.GetCurrentPrice(request);

        result.Name.Should().Be("First Fetcher");
    }

    [Fact]
    public void GetCurrentPrice_CryptocurrencyRequest_ReachesRealCryptocurrencyAssetPriceFetcher()
    {
        var fetchers = new IAssetPriceFetcher[]
        {
            new StandardAssetPriceFetcher(new StubFinanceService()),
            new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService())
        };
        var service = new AssetPriceService(fetchers);
        var request = new AssetPriceRequestDTO
        {
            Exchange = "",
            Ticker = "BTC",
            AssetClass = GlobalAssetClass.Cryptocurrency,
            BrokerName = "NotABroker"
        };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*NotABroker*");
    }

    [Fact]
    public void GetCurrentPrice_NonCryptocurrencyRequest_ReachesRealStandardAssetPriceFetcher()
    {
        var fetchers = new IAssetPriceFetcher[]
        {
            new StandardAssetPriceFetcher(new StubFinanceService()),
            new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService())
        };
        var service = new AssetPriceService(fetchers);
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BCIA11", AssetClass = GlobalAssetClass.Equity };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>().WithMessage("Exchange is required.*");
    }

    private sealed class StubFetcher : IAssetPriceFetcher
    {
        private readonly Func<GlobalAssetClass, bool> _supports;
        private readonly AssetValueSnapshot _snapshot;

        public StubFetcher(Func<GlobalAssetClass, bool> supports, AssetValueSnapshot snapshot)
        {
            _supports = supports;
            _snapshot = snapshot;
        }

        public bool Supports(GlobalAssetClass assetClass) => _supports(assetClass);

        public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request) => _snapshot;
    }
}
