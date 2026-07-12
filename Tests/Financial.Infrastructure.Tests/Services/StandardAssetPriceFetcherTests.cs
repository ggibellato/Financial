using Financial.Application.DTOs;
using Financial.Domain.Entities;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class StandardAssetPriceFetcherTests
{
    [Fact]
    public void Supports_Cryptocurrency_ReturnsFalse()
    {
        var fetcher = new StandardAssetPriceFetcher();

        var result = fetcher.Supports(GlobalAssetClass.Cryptocurrency);

        result.Should().BeFalse();
    }

    [Fact]
    public void Supports_Equity_ReturnsTrue()
    {
        var fetcher = new StandardAssetPriceFetcher();

        var result = fetcher.Supports(GlobalAssetClass.Equity);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Unknown_ReturnsTrue()
    {
        var fetcher = new StandardAssetPriceFetcher();

        var result = fetcher.Supports(GlobalAssetClass.Unknown);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Bond_ReturnsTrue()
    {
        var fetcher = new StandardAssetPriceFetcher();

        var result = fetcher.Supports(GlobalAssetClass.Bond);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetSnapshot_BlankExchange_ThrowsArgumentException()
    {
        var fetcher = new StandardAssetPriceFetcher();
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "BCIA11" };

        Action act = () => fetcher.GetSnapshot(request);

        act.Should().Throw<ArgumentException>().WithMessage("Exchange is required.*");
    }
}
