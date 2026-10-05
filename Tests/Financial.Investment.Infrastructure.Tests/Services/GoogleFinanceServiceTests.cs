using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Integration")]
public class GoogleFinanceServiceTests
{
    /// <summary>The default service the request-validation tests exercise; the parsing tests inject their own transport.</summary>
    private readonly GoogleFinanceService _sut;

    public GoogleFinanceServiceTests()
    {
        _sut = new GoogleFinanceService();
    }

    [Fact]
    public void GetAssetValue_BlankTicker_ThrowsArgumentException()
    {
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => _sut.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Ticker is required.*");
    }

    [Fact]
    public void GetAssetValue_NeitherExchangeNorCurrencyProvided_ThrowsArgumentException()
    {
        var request = new AssetValueRequestDTO { Ticker = "BCIA11" };

        Action act = () => _sut.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Either Exchange or Currency must be provided.*");
    }

    [Fact]
    public void GetAssetValue_ExchangeProvided_DelegatesToExchangeLookup()
    {
        var snapshot = new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, TestClock.Default);
        var service = new GoogleFinanceService(
            (exchange, ticker) => exchange == "BVMF" && ticker == "BCIA11" ? snapshot : throw new InvalidOperationException(),
            (_, _) => throw new InvalidOperationException("crypto lookup should not be called"));
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BCIA11" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }

    [Fact]
    public void GetAssetValue_CurrencyProvidedWithoutExchange_DelegatesToCryptoLookup()
    {
        var snapshot = new AssetValueSnapshot("BTC", "Bitcoin", 50000m, TestClock.Default);
        var service = new GoogleFinanceService(
            (_, _) => throw new InvalidOperationException("exchange lookup should not be called"),
            (currency, ticker) => currency == "GBP" && ticker == "BTC" ? snapshot : throw new InvalidOperationException());
        var request = new AssetValueRequestDTO { Currency = "GBP", Ticker = "BTC" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }
}
