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
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Action act = () => new CryptocurrencyAssetPriceFetcher(null!, new StubFinanceService());

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullFinanceService_ThrowsArgumentNullException()
    {
        Action act = () => new CryptocurrencyAssetPriceFetcher(new StubRepository([]), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("financeService");
    }

    [Fact]
    public void Supports_Cryptocurrency_ReturnsTrue()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService());

        var result = fetcher.Supports(GlobalAssetClass.Cryptocurrency);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Equity_ReturnsFalse()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService());

        var result = fetcher.Supports(GlobalAssetClass.Equity);

        result.Should().BeFalse();
    }

    [Fact]
    public void Supports_Unknown_ReturnsFalse()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService());

        var result = fetcher.Supports(GlobalAssetClass.Unknown);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_BlankBrokerName_ThrowsArgumentException()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService());
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BTC", AssetClass = GlobalAssetClass.Cryptocurrency, BrokerName = null };

        Action act = () => fetcher.GetSnapshot(request);

        act.Should().Throw<ArgumentException>().WithMessage("BrokerName is required for cryptocurrency assets.*");
    }

    [Fact]
    public void GetSnapshot_UnknownBroker_ThrowsInvalidOperationException()
    {
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository([]), new StubFinanceService());
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
        var fetcher = new CryptocurrencyAssetPriceFetcher(new StubRepository(brokers), new StubFinanceService(snapshot));
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

}
