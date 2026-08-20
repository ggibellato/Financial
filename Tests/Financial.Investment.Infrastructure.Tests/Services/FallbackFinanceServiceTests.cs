using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class FallbackFinanceServiceTests
{
    private readonly RecordingLogger<FallbackFinanceService> _logger;

    public FallbackFinanceServiceTests()
    {
        _logger = new RecordingLogger<FallbackFinanceService>();
    }

    /// <summary>Wires the SUT over the shared recording logger; the two providers' behaviour is what
    /// each test varies.</summary>
    private FallbackFinanceService CreateService(IFinanceService primary, IFinanceService fallback) =>
        new(primary, fallback, _logger);

    /// <summary>A fallback provider that fails the test if the SUT reaches it at all.</summary>
    private static FakeFinanceService UnreachableFallback() =>
        new(_ => throw new InvalidOperationException("fallback should not be called"));

    [Fact]
    public void GetAssetValue_PrimarySucceeds_ReturnsPrimarySnapshot_AndNeverCallsFallback()
    {
        var snapshot = new AssetValueSnapshot("BBAS3", "Banco do Brasil", 19.17m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => snapshot);
        var fallback = UnreachableFallback();
        var service = CreateService(primary, fallback);
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
        var service = CreateService(primary, fallback);
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }

    [Fact]
    public void GetAssetValue_BothProvidersFail_ThrowsInvalidOperationExceptionWrappingBoth()
    {
        var primary = new FakeFinanceService(_ => throw new InvalidOperationException("google failed"));
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("yahoo failed"));
        var service = CreateService(primary, fallback);
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
        var fallback = UnreachableFallback();
        var service = CreateService(primary, fallback);
        var request = new AssetValueRequestDTO { Currency = "GBP", Ticker = "BTC" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("crypto lookup failed");
    }

    [Fact]
    public void GetAssetValue_PrimaryThrowsArgumentException_PropagatesWithoutCallingFallback()
    {
        var primary = new FakeFinanceService(_ => throw new ArgumentException("Ticker is required."));
        var fallback = UnreachableFallback();
        var service = CreateService(primary, fallback);
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Ticker is required.*");
    }

    [Fact]
    public void GetAssetValue_FallbackEngages_LogsWarningWithTickerAndErrorType_NotTheMessage()
    {
        var snapshot = new AssetValueSnapshot("BBAS3F", "Banco do Brasil", 21.18m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => throw new InvalidOperationException("Google Finance main data node not found."));
        var fallback = new FakeFinanceService(_ => snapshot);
        var service = CreateService(primary, fallback);

        service.GetAssetValue(new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" });

        var warning = _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning).Which;
        warning.Message.Should().Contain("BBAS3F");
        warning.Message.Should().Contain(nameof(InvalidOperationException));
        warning.Message.Should().NotContain("main data node", "provider exception messages are not logged");
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information, "fallback success is logged");
    }

    [Fact]
    public void GetAssetValue_PrimarySucceeds_LogsNothing()
    {
        var snapshot = new AssetValueSnapshot("BBAS3", "Banco do Brasil", 19.17m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => snapshot);
        var fallback = UnreachableFallback();
        var service = CreateService(primary, fallback);

        service.GetAssetValue(new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3" });

        _logger.Entries.Should().BeEmpty();
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
