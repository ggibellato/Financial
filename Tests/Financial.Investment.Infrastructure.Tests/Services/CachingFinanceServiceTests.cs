using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Unit")]
public class CachingFinanceServiceTests
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly FakeTimeProvider _clock;
    private readonly SequenceFinanceService _inner;
    private readonly CachingFinanceService _sut;

    public CachingFinanceServiceTests()
    {
        _clock = TestClock.At();
        _inner = new SequenceFinanceService();
        _sut = new CachingFinanceService(_inner, _clock, CacheDuration);
    }

    private static AssetValueRequestDTO StockRequest(string ticker = "BBAS3", string exchange = "BVMF") =>
        new() { Exchange = exchange, Ticker = ticker };

    [Fact]
    public void GetAssetValue_SameQuoteWithinCacheDuration_ReturnsCachedSnapshotWithoutRefetching()
    {
        var first = _sut.GetAssetValue(StockRequest());
        _clock.Advance(CacheDuration - TimeSpan.FromSeconds(1));

        var second = _sut.GetAssetValue(StockRequest());

        second.Should().BeSameAs(first);
        _inner.Calls.Should().Be(1);
    }

    [Fact]
    public void GetAssetValue_CacheDurationElapsed_FetchesFreshSnapshot()
    {
        _sut.GetAssetValue(StockRequest());
        _clock.Advance(CacheDuration);

        var second = _sut.GetAssetValue(StockRequest());

        second.Price.Should().Be(2m);
        _inner.Calls.Should().Be(2);
    }

    [Fact]
    public void GetAssetValue_InnerFails_FailureIsNotCachedAndNextCallRetries()
    {
        _inner.FailNextCall(new InvalidOperationException("provider down"));
        Action failing = () => _sut.GetAssetValue(StockRequest());
        failing.Should().Throw<InvalidOperationException>().WithMessage("provider down");

        var retried = _sut.GetAssetValue(StockRequest());

        retried.Price.Should().Be(2m);
        _inner.Calls.Should().Be(2);
    }

    [Fact]
    public void GetAssetValue_FetchFailsAfterExpiry_DoesNotServeTheStaleSnapshot()
    {
        _sut.GetAssetValue(StockRequest());
        _clock.Advance(CacheDuration);
        _inner.FailNextCall(new InvalidOperationException("provider down"));

        Action act = () => _sut.GetAssetValue(StockRequest());

        act.Should().Throw<InvalidOperationException>().WithMessage("provider down");
    }

    [Theory]
    [InlineData("PETR4", "BVMF", null, null)]
    [InlineData("BBAS3", "NYSE", null, null)]
    [InlineData("BBAS3", "BVMF", "GBP", null)]
    [InlineData("BBAS3", "BVMF", null, "Tesouro IPCA+ 2035")]
    public void GetAssetValue_DifferentQuoteIdentity_FetchesSeparately(string ticker, string exchange, string? currency, string? name)
    {
        _sut.GetAssetValue(StockRequest());

        var other = _sut.GetAssetValue(new AssetValueRequestDTO { Ticker = ticker, Exchange = exchange, Currency = currency, Name = name });

        other.Price.Should().Be(2m);
        _inner.Calls.Should().Be(2);
    }

    [Fact]
    public void GetAssetValue_ZeroCacheDuration_AlwaysFetches()
    {
        var sut = new CachingFinanceService(_inner, _clock, TimeSpan.Zero);
        sut.GetAssetValue(StockRequest());

        sut.GetAssetValue(StockRequest());

        _inner.Calls.Should().Be(2);
    }

    private sealed class SequenceFinanceService : IFinanceService
    {
        private Exception? _nextFailure;

        public int Calls { get; private set; }

        public void FailNextCall(Exception failure) => _nextFailure = failure;

        public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
        {
            Calls++;
            if (_nextFailure is { } failure)
            {
                _nextFailure = null;
                throw failure;
            }

            return new AssetValueSnapshot(request.Ticker!, "Name", Calls, TestClock.Default);
        }
    }
}
