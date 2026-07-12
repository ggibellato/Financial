using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class CryptocurrencyAssetPriceFetcherTests
{
    [Fact]
    public void Supports_Cryptocurrency_ReturnsTrue()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService());

        var result = fetcher.Supports(GlobalAssetClass.Cryptocurrency);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Equity_ReturnsFalse()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService());

        var result = fetcher.Supports(GlobalAssetClass.Equity);

        result.Should().BeFalse();
    }

    [Fact]
    public void Supports_Unknown_ReturnsFalse()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService());

        var result = fetcher.Supports(GlobalAssetClass.Unknown);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_BlankBrokerName_ThrowsArgumentException()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService());
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BTC", AssetClass = GlobalAssetClass.Cryptocurrency, BrokerName = null };

        Action act = () => fetcher.GetSnapshot(request);

        act.Should().Throw<ArgumentException>().WithMessage("BrokerName is required for cryptocurrency assets.*");
    }

    [Fact]
    public void GetSnapshot_UnknownBroker_ThrowsInvalidOperationException()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new FakeFinanceService());
        var request = new AssetPriceRequestDTO
        {
            Exchange = "",
            Ticker = "BTC",
            AssetClass = GlobalAssetClass.Cryptocurrency,
            BrokerName = "NotABroker"
        };

        Action act = () => fetcher.GetSnapshot(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("*NotABroker*");
    }

    [Fact]
    public void GetSnapshot_KnownBroker_DelegatesToFinanceServiceWithResolvedCurrency()
    {
        var snapshot = new AssetValueSnapshot("BTC", "Bitcoin", 50000m, DateTimeOffset.UtcNow);
        var brokers = new[] { Broker.Create("Coinbase", "GBP") };
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository(brokers), new FakeFinanceService(snapshot));
        var request = new AssetPriceRequestDTO
        {
            Exchange = "",
            Ticker = "BTC",
            AssetClass = GlobalAssetClass.Cryptocurrency,
            BrokerName = "Coinbase"
        };

        var result = fetcher.GetSnapshot(request);

        result.Should().Be(snapshot);
    }

    [Fact]
    public void ResolveBrokerCurrency_KnownBroker_ReturnsCurrency()
    {
        var brokers = new[] { Broker.Create("Coinbase", "GBP") };

        var currency = CryptocurrencyAssetPriceFetcher.ResolveBrokerCurrency(brokers, "Coinbase");

        currency.Should().Be("GBP");
    }

    [Fact]
    public void ResolveBrokerCurrency_UnknownBroker_ThrowsInvalidOperationException()
    {
        Action act = () => CryptocurrencyAssetPriceFetcher.ResolveBrokerCurrency([], "Coinbase");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Coinbase*");
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
        private readonly AssetValueSnapshot? _snapshot;

        public FakeFinanceService(AssetValueSnapshot? snapshot = null)
        {
            _snapshot = snapshot;
        }

        public AssetValueSnapshot GetQuote(FinanceQuoteRequest request) => _snapshot ?? throw new NotImplementedException();
    }
}
