using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Unit")]
public class FallbackFinanceServiceProviderMemoryTests
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);

    private readonly FakeTimeProvider _clock;
    private readonly RecordingLogger<FallbackFinanceService> _logger;
    private readonly CountingFinanceService _primary;
    private readonly CountingFinanceService _fallback;
    private readonly FallbackFinanceService _sut;

    public FallbackFinanceServiceProviderMemoryTests()
    {
        _clock = TestClock.At();
        _logger = new RecordingLogger<FallbackFinanceService>();
        _primary = new CountingFinanceService(price: 1m);
        _fallback = new CountingFinanceService(price: 2m);
        _sut = new FallbackFinanceService(_primary, _fallback, _clock, Cooldown, _logger);
    }

    private static AssetValueRequestDTO StockRequest(string ticker = "BBAS3F") =>
        new() { Exchange = "BVMF", Ticker = ticker };

    [Fact]
    public void GetAssetValue_AfterFallbackSucceeded_TriesFallbackFirstAndSkipsFailingPrimary()
    {
        _primary.FailWith(new InvalidOperationException("not tracked"));
        _sut.GetAssetValue(StockRequest());

        var result = _sut.GetAssetValue(StockRequest());

        result.Price.Should().Be(2m);
        _primary.Calls.Should().Be(1);
        _fallback.Calls.Should().Be(2);
    }

    [Fact]
    public void GetAssetValue_PrimaryFailedForAnotherTicker_StillTriesPrimaryFirst()
    {
        _primary.FailWith(new InvalidOperationException("not tracked"));
        _sut.GetAssetValue(StockRequest("BBAS3F"));
        _primary.Succeed();

        var result = _sut.GetAssetValue(StockRequest("PETR4"));

        result.Price.Should().Be(1m);
        _fallback.Calls.Should().Be(1);
    }

    [Fact]
    public void GetAssetValue_CooldownStillRunning_SkipsPrimaryJustBeforeExpiry()
    {
        _primary.FailWith(new InvalidOperationException("not tracked"));
        _sut.GetAssetValue(StockRequest());
        _primary.Succeed();
        _clock.Advance(Cooldown - TimeSpan.FromSeconds(1));

        var result = _sut.GetAssetValue(StockRequest());

        result.Price.Should().Be(2m);
        _primary.Calls.Should().Be(1);
    }

    [Fact]
    public void GetAssetValue_CooldownExpired_TriesPrimaryFirstAgain()
    {
        _primary.FailWith(new InvalidOperationException("not tracked"));
        _sut.GetAssetValue(StockRequest());
        _primary.Succeed();
        _clock.Advance(Cooldown);

        var result = _sut.GetAssetValue(StockRequest());

        result.Price.Should().Be(1m);
        _primary.Calls.Should().Be(2);
        _fallback.Calls.Should().Be(1);
    }

    [Fact]
    public void GetAssetValue_FallbackPreferredButNowFails_StillFallsBackToCoolingPrimary()
    {
        _primary.FailWith(new InvalidOperationException("not tracked"));
        _sut.GetAssetValue(StockRequest());
        _primary.Succeed();
        _fallback.FailWith(new HttpRequestException("yahoo down"));

        var result = _sut.GetAssetValue(StockRequest());

        result.Price.Should().Be(1m);
        _fallback.Calls.Should().Be(2);
        _primary.Calls.Should().Be(2);
    }

    [Fact]
    public void GetAssetValue_AllProvidersFailWhileFallbackPreferred_ThrowsSameAggregateAsPrimaryFirst()
    {
        var primaryFailure = new InvalidOperationException("google failed");
        var fallbackFailure = new HttpRequestException("yahoo failed");
        _primary.FailWith(primaryFailure);
        _sut.GetAssetValue(StockRequest());
        _fallback.FailWith(fallbackFailure);

        Action act = () => _sut.GetAssetValue(StockRequest());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Both finance providers failed to fetch a price for ticker 'BBAS3F'.")
            .Which.InnerException.Should().BeOfType<AggregateException>()
            .Which.InnerExceptions.Should().Equal(primaryFailure, fallbackFailure);
    }

    [Fact]
    public void GetAssetValue_PrimaryThrowsNonProviderError_DoesNotStartCooldown()
    {
        _primary.FailWith(new ArgumentException("Ticker is required."));
        Action first = () => _sut.GetAssetValue(StockRequest());
        first.Should().Throw<ArgumentException>();
        _primary.Succeed();

        var result = _sut.GetAssetValue(StockRequest());

        result.Price.Should().Be(1m);
        _fallback.Calls.Should().Be(0);
    }

    [Fact]
    public void GetAssetValue_PreferredFallbackFails_LogsWarningWithErrorTypeNotMessage()
    {
        _primary.FailWith(new InvalidOperationException("not tracked"));
        _sut.GetAssetValue(StockRequest());
        _primary.Succeed();
        _fallback.FailWith(new HttpRequestException("yahoo secret detail"));

        _sut.GetAssetValue(StockRequest());

        var warning = _logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).Last();
        warning.Message.Should().Contain("BBAS3F").And.Contain(nameof(HttpRequestException));
        warning.Message.Should().NotContain("secret detail");
    }

    private sealed class CountingFinanceService : IFinanceService
    {
        private readonly decimal _price;
        private Exception? _failure;

        public CountingFinanceService(decimal price)
        {
            _price = price;
        }

        public int Calls { get; private set; }

        public void FailWith(Exception failure) => _failure = failure;

        public void Succeed() => _failure = null;

        public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
        {
            Calls++;
            if (_failure is not null)
            {
                throw _failure;
            }

            return new AssetValueSnapshot(request.Ticker!, "Name", _price, TestClock.Default);
        }
    }
}
