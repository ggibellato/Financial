using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class AssetPriceServiceTests
{
    /// <summary>The fetcher-less service the guard-clause tests exercise; the dispatch tests build their own over specific fetchers.</summary>
    private readonly AssetPriceService _serviceWithoutFetchers;

    public AssetPriceServiceTests()
    {
        _serviceWithoutFetchers = new AssetPriceService([]);
    }

    [Fact]
    public void GetCurrentPrice_NullRequest_ThrowsArgumentNullException()
    {
        Action act = () => _serviceWithoutFetchers.GetCurrentPrice(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCurrentPrice_BlankTicker_ThrowsArgumentException()
    {
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => _serviceWithoutFetchers.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetCurrentPrice_NoFetchersRegistered_ThrowsInvalidOperationException()
    {
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "BCIA11" };

        Action act = () => _serviceWithoutFetchers.GetCurrentPrice(request);

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
    public void GetCurrentPrice_NoFetcherSupportsAssetClass_ThrowsNamingTheClass()
    {
        var snapshot = new AssetValueSnapshot("XXX", "First Fetcher", 1m, DateTimeOffset.UtcNow);
        var service = new AssetPriceService([new StubFetcher(_ => false, snapshot), new StubFetcher(_ => false, snapshot)]);
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "XXX", AssetClass = GlobalAssetClass.PrivateCredit };

        var act = () => service.GetCurrentPrice(request);

        act.Should().Throw<NotSupportedException>().WithMessage("*PrivateCredit*");
    }

    /// <summary>
    /// Falling back to the first registered fetcher meant an unsupported class was looked up as
    /// something it is not - a private-credit holding asked for as an equity ticker - and the
    /// resulting provider error hid the real cause.
    /// </summary>
    [Fact]
    public void GetCurrentPrice_NoFetcherSupportsAssetClass_DoesNotCallAnyFetcher()
    {
        var snapshot = new AssetValueSnapshot("XXX", "First Fetcher", 1m, DateTimeOffset.UtcNow);
        var fetcher = new StubFetcher(_ => false, snapshot);
        var service = new AssetPriceService([fetcher]);
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "XXX", AssetClass = GlobalAssetClass.Pension };

        var act = () => service.GetCurrentPrice(request);

        act.Should().Throw<NotSupportedException>();
        fetcher.SnapshotCallCount.Should().Be(0);
    }

    [Fact]
    public void GetCurrentPrice_CryptocurrencyRequest_ReachesRealCryptocurrencyAssetPriceFetcher()
    {
        var fetchers = new IAssetPriceFetcher[]
        {
            new StandardAssetPriceFetcher(new StubFinanceService()),
            new CryptocurrencyAssetPriceFetcher(new StubInvestmentRepository([]), new StubFinanceService())
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
            new CryptocurrencyAssetPriceFetcher(new StubInvestmentRepository([]), new StubFinanceService())
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

        public int SnapshotCallCount { get; private set; }

        public bool Supports(GlobalAssetClass assetClass) => _supports(assetClass);

        public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
        {
            SnapshotCallCount++;
            return _snapshot;
        }
    }
}
