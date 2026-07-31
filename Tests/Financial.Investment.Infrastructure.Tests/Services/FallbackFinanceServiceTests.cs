using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class FallbackFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_PrimarySucceeds_ReturnsPrimarySnapshot_AndNeverCallsFallback()
    {
        var snapshot = new AssetValueSnapshot("BBAS3", "Banco do Brasil", 19.17m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => snapshot);
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("fallback should not be called"));
        var service = new FallbackFinanceService(primary, fallback);
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }

    [Fact]
    public void GetAssetValue_PrimaryFailsForStockLookup_FallsBackAndReturnsFallbackSnapshot()
    {
        var snapshot = new AssetValueSnapshot("BBAS3F", "Banco do Brasil", 21.18m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => throw new InvalidOperationException("Google Finance main data node not found."));
        var fallback = new FakeFinanceService(_ => snapshot);
        var service = new FallbackFinanceService(primary, fallback);
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }

    [Fact]
    public void GetAssetValue_BothProvidersFail_ThrowsInvalidOperationExceptionWrappingBoth()
    {
        var primary = new FakeFinanceService(_ => throw new InvalidOperationException("google failed"));
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("yahoo failed"));
        var service = new FallbackFinanceService(primary, fallback);
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "UNKNOWN" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Both finance providers failed*")
            .Which.InnerException.Should().BeOfType<AggregateException>()
            .Which.InnerExceptions.Should().HaveCount(2);
    }

    [Fact]
    public void GetAssetValue_PrimaryFailsForCryptoLookup_PropagatesWithoutCallingFallback()
    {
        var primary = new FakeFinanceService(_ => throw new InvalidOperationException("crypto lookup failed"));
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("fallback should not be called"));
        var service = new FallbackFinanceService(primary, fallback);
        var request = new AssetValueRequestDTO { Currency = "GBP", Ticker = "BTC" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("crypto lookup failed");
    }

    [Fact]
    public void GetAssetValue_PrimaryThrowsArgumentException_PropagatesWithoutCallingFallback()
    {
        var primary = new FakeFinanceService(_ => throw new ArgumentException("Ticker is required."));
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("fallback should not be called"));
        var service = new FallbackFinanceService(primary, fallback);
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Ticker is required.*");
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
