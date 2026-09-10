using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class BondAssetPriceFetcherTests
{
    /// <summary>Every test drives the same BondAssetPriceFetcher, so it is wired once here.</summary>
    private readonly BondAssetPriceFetcher _sut;

    public BondAssetPriceFetcherTests()
    {
        _sut = new BondAssetPriceFetcher(new StatusInvestFinanceService(_ => throw new NotImplementedException()));
    }

    [Fact]
    public void Constructor_WithNullStatusInvestFinanceService_ThrowsArgumentNullException()
    {
        Action act = () => new BondAssetPriceFetcher(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("financeService");
    }

    [Fact]
    public void Supports_Bond_ReturnsTrue()
    {
        var result = _sut.Supports(GlobalAssetClass.Bond);

        result.Should().BeTrue();
    }

    [Fact]
    public void Supports_Equity_ReturnsFalse()
    {
        var result = _sut.Supports(GlobalAssetClass.Equity);

        result.Should().BeFalse();
    }

    [Fact]
    public void Supports_Cryptocurrency_ReturnsFalse()
    {
        var result = _sut.Supports(GlobalAssetClass.Cryptocurrency);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_BlankName_ThrowsArgumentException()
    {
        var request = new AssetPriceRequestDTO { Exchange = "", Ticker = "TESOURO IPCA+ 2029", Name = "" };

        Action act = () => _sut.GetSnapshot(request);

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

    [Fact]
    public void GetSnapshot_ValidRequest_ForwardsExchangeAndTickerToFinanceService()
    {
        AssetValueRequestDTO? captured = null;
        var snapshot = new AssetValueSnapshot("TESOURO IPCA+ 2029", "TESOURO IPCA+ 2029", 3775.97m, DateTimeOffset.UtcNow);
        var fetcher = new BondAssetPriceFetcher(new FakeFinanceService(request =>
        {
            captured = request;
            return snapshot;
        }));
        var request = new AssetPriceRequestDTO { Exchange = "BVMF", Ticker = "TESOURO IPCA+ 2029", Name = "TESOURO IPCA+ 2029" };

        fetcher.GetSnapshot(request);

        captured.Should().NotBeNull();
        captured!.Exchange.Should().Be("BVMF");
        captured.Ticker.Should().Be("TESOURO IPCA+ 2029");
        captured.Name.Should().Be("TESOURO IPCA+ 2029");
    }

    private sealed class FakeFinanceService : IFinanceService
    {
        private readonly Func<AssetValueRequestDTO, AssetValueSnapshot> _behavior;

        public FakeFinanceService(Func<AssetValueRequestDTO, AssetValueSnapshot> behavior)
        {
            _behavior = behavior;
        }

        public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request) => _behavior(request);
    }
}
