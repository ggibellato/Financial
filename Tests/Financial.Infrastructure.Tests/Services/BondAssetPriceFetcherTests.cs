using Financial.Application.DTOs;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class BondAssetPriceFetcherTests
{
    [Fact]
    public void Supports_Bond_ReturnsTrue()
    {
        var fetcher = new BondAssetPriceFetcher(new StatusInvestFinanceService(_ => throw new NotImplementedException()));

        var result = fetcher.Supports(GlobalAssetClass.Bond);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Equity_ReturnsFalse()
    {
        var fetcher = new BondAssetPriceFetcher(new StatusInvestFinanceService(_ => throw new NotImplementedException()));

        var result = fetcher.Supports(GlobalAssetClass.Equity);

        result.Should().BeFalse();
    }

    [Fact]
    public void Supports_Cryptocurrency_ReturnsFalse()
    {
        var fetcher = new BondAssetPriceFetcher(new StatusInvestFinanceService(_ => throw new NotImplementedException()));

        var result = fetcher.Supports(GlobalAssetClass.Cryptocurrency);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_BlankName_ThrowsArgumentException()
    {
        var fetcher = new BondAssetPriceFetcher(new StatusInvestFinanceService(_ => throw new NotImplementedException()));
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "TESOURO IPCA+ 2029", Name = "" };

        Action act = () => fetcher.GetSnapshot(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required for bond assets.*");
    }

    [Fact]
    public void GetSnapshot_ValidName_DelegatesToStatusInvestFinanceService()
    {
        var snapshot = new AssetValueSnapshot("TESOURO IPCA+ 2029", "TESOURO IPCA+ 2029", 3775.97m, DateTimeOffset.UtcNow);
        var fetcher = new BondAssetPriceFetcher(new StatusInvestFinanceService(_ => snapshot));
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "TESOURO IPCA+ 2029", Name = "TESOURO IPCA+ 2029" };

        var result = fetcher.GetSnapshot(request);

        result.Should().Be(snapshot);
    }
}
