using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class StandardAssetPriceFetcherTests
{
    /// <summary>Every test drives the same StandardAssetPriceFetcher, so it is wired once here.</summary>
    private readonly StandardAssetPriceFetcher _sut;

    public StandardAssetPriceFetcherTests()
    {
        _sut = new StandardAssetPriceFetcher(new StubFinanceService());
    }

    [Fact]
    public void Constructor_WithNullFinanceService_ThrowsArgumentNullException()
    {
        Action act = () => new StandardAssetPriceFetcher(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("financeService");
    }

    [Fact]
    public void Supports_Cryptocurrency_ReturnsFalse()
    {
        var result = _sut.Supports(GlobalAssetClass.Cryptocurrency);

        result.Should().BeFalse();
    }

    [Fact]
    public void Supports_Equity_ReturnsTrue()
    {
        var result = _sut.Supports(GlobalAssetClass.Equity);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Unknown_ReturnsTrue()
    {
        var result = _sut.Supports(GlobalAssetClass.Unknown);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Bond_ReturnsFalse()
    {
        var result = _sut.Supports(GlobalAssetClass.Bond);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_BlankExchange_ThrowsArgumentException()
    {
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BCIA11" };

        Action act = () => _sut.GetSnapshot(request);

        act.Should().Throw<ArgumentException>().WithMessage("Exchange is required.*");
    }

    [Fact]
    public void GetSnapshot_ValidExchange_DelegatesToFinanceService()
    {
        var snapshot = new AssetValueSnapshot("BCIA11", "Some ETF", 10.5m, DateTimeOffset.UtcNow);
        var fetcher = new StandardAssetPriceFetcher(new StubFinanceService(snapshot));
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "BCIA11" };

        var result = fetcher.GetSnapshot(request);

        result.Should().Be(snapshot);
    }
}
