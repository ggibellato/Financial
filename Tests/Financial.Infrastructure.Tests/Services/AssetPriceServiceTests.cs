using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class AssetPriceServiceTests
{
    [Fact]
    public void GetCurrentPrice_NullRequest_ThrowsArgumentNullException()
    {
        var service = new AssetPriceService(new StubRepository([]));

        Action act = () => service.GetCurrentPrice(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCurrentPrice_BlankTicker_ThrowsArgumentException()
    {
        var service = new AssetPriceService(new StubRepository([]));
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetCurrentPrice_NonCryptocurrencyBlankExchange_ThrowsArgumentException()
    {
        var service = new AssetPriceService(new StubRepository([]));
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BCIA11", AssetClass = GlobalAssetClass.Unknown };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetCurrentPrice_CryptocurrencyBlankBrokerName_ThrowsArgumentException()
    {
        var service = new AssetPriceService(new StubRepository([]));
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BTC", AssetClass = GlobalAssetClass.Cryptocurrency, BrokerName = null };

        Action act = () => service.GetCurrentPrice(request);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetCurrentPrice_CryptocurrencyUnknownBroker_ThrowsInvalidOperationException()
    {
        var service = new AssetPriceService(new StubRepository([]));
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
    public void ResolveBrokerCurrency_KnownBroker_ReturnsCurrency()
    {
        var brokers = new[] { Broker.Create("Coinbase", "GBP") };

        var currency = AssetPriceService.ResolveBrokerCurrency(brokers, "Coinbase");

        currency.Should().Be("GBP");
    }

    [Fact]
    public void ResolveBrokerCurrency_UnknownBroker_ThrowsInvalidOperationException()
    {
        Action act = () => AssetPriceService.ResolveBrokerCurrency([], "Coinbase");

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
}
