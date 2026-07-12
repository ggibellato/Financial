using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
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
    public void GetCurrentPrice_CryptocurrencyAssetClass_DispatchesToMatchingFetcher()
    {
        var cryptoSnapshot = new AssetValueSnapshot("BTC", "Bitcoin", 50000m, DateTimeOffset.UtcNow);
        var standardSnapshot = new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, DateTimeOffset.UtcNow);
        var standardFetcher = new FakeFetcher(assetClass => assetClass != GlobalAssetClass.Cryptocurrency, standardSnapshot);
        var cryptoFetcher = new FakeFetcher(assetClass => assetClass == GlobalAssetClass.Cryptocurrency, cryptoSnapshot);
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
        var standardFetcher = new FakeFetcher(assetClass => assetClass != GlobalAssetClass.Cryptocurrency, standardSnapshot);
        var cryptoFetcher = new FakeFetcher(assetClass => assetClass == GlobalAssetClass.Cryptocurrency, cryptoSnapshot);
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
        var firstFetcher = new FakeFetcher(_ => false, firstSnapshot);
        var secondFetcher = new FakeFetcher(_ => false, secondSnapshot);
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
            new StandardAssetPriceFetcher(new FakeFinanceService()),
            new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService())
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
            new StandardAssetPriceFetcher(new FakeFinanceService()),
            new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService())
        };
        var service = new AssetPriceService(fetchers);
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BCIA11", AssetClass = GlobalAssetClass.Equity };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>().WithMessage("Exchange is required.*");
    }

    private sealed class FakeFetcher : IAssetPriceFetcher
    {
        private readonly Func<GlobalAssetClass, bool> _supports;
        private readonly AssetValueSnapshot _snapshot;

        public FakeFetcher(Func<GlobalAssetClass, bool> supports, AssetValueSnapshot snapshot)
        {
            _supports = supports;
            _snapshot = snapshot;
        }

        public bool Supports(GlobalAssetClass assetClass) => _supports(assetClass);

        public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request) => _snapshot;
    }

    private sealed class StubRepository : IRepository
    {
        private readonly List<Broker> _brokers;

        public StubRepository(IEnumerable<Broker> brokers)
        {
            _brokers = brokers.ToList();
        }

        public IEnumerable<Asset> GetAssetsByBroker(string name) => throw new NotImplementedException();

        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio) => throw new NotImplementedException();

        public IEnumerable<Broker> GetBrokerList() => _brokers;

        public Asset? GetAsset(string brokerName, string portfolioName, string assetName) => throw new NotImplementedException();

        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class FakeFinanceService : IFinanceService
    {
        public AssetValueSnapshot GetAssetValue(AssetValueRequest request) => throw new NotImplementedException();
    }
}
