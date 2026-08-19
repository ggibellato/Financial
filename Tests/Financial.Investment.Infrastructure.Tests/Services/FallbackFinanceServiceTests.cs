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
    [Fact]
    public void GetAssetValue_PrimarySucceeds_ReturnsPrimarySnapshot_AndNeverCallsFallback()
    {
        var snapshot = new AssetValueSnapshot("BBAS3", "Banco do Brasil", 19.17m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => snapshot);
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("fallback should not be called"));
        var service = new FallbackFinanceService(primary, fallback, new RecordingLogger<FallbackFinanceService>());
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
        var service = new FallbackFinanceService(primary, fallback, new RecordingLogger<FallbackFinanceService>());
        var request = new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }

    [Fact]
    public void GetAssetValue_BothProvidersFail_ThrowsInvalidOperationExceptionWrappingBoth()
    {
        var primary = new FakeFinanceService(_ => throw new InvalidOperationException("google failed"));
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("yahoo failed"));
        var service = new FallbackFinanceService(primary, fallback, new RecordingLogger<FallbackFinanceService>());
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
        var service = new FallbackFinanceService(primary, fallback, new RecordingLogger<FallbackFinanceService>());
        var request = new AssetValueRequestDTO { Currency = "GBP", Ticker = "BTC" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<InvalidOperationException>().WithMessage("crypto lookup failed");
    }

    [Fact]
    public void GetAssetValue_PrimaryThrowsArgumentException_PropagatesWithoutCallingFallback()
    {
        var primary = new FakeFinanceService(_ => throw new ArgumentException("Ticker is required."));
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("fallback should not be called"));
        var service = new FallbackFinanceService(primary, fallback, new RecordingLogger<FallbackFinanceService>());
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
        var logger = new RecordingLogger<FallbackFinanceService>();
        var service = new FallbackFinanceService(primary, fallback, logger);

        service.GetAssetValue(new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3F" });

        var warning = logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning).Which;
        warning.Message.Should().Contain("BBAS3F");
        warning.Message.Should().Contain(nameof(InvalidOperationException));
        warning.Message.Should().NotContain("main data node", "provider exception messages are not logged");
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Information, "fallback success is logged");
    }

    [Fact]
    public void GetAssetValue_PrimarySucceeds_LogsNothing()
    {
        var snapshot = new AssetValueSnapshot("BBAS3", "Banco do Brasil", 19.17m, DateTimeOffset.UtcNow);
        var primary = new FakeFinanceService(_ => snapshot);
        var fallback = new FakeFinanceService(_ => throw new InvalidOperationException("fallback should not be called"));
        var logger = new RecordingLogger<FallbackFinanceService>();
        var service = new FallbackFinanceService(primary, fallback, logger);

        service.GetAssetValue(new AssetValueRequestDTO { Exchange = "BVMF", Ticker = "BBAS3" });

        logger.Entries.Should().BeEmpty();
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
