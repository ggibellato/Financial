using System.Net;
using System.Net.Http;
using Financial.Integrations.GoogleCore;
using FluentAssertions;
using Google;

namespace Financial.GoogleIntegrations.Tests;

[Trait("Category", "Unit")]
public class GoogleRetryPolicyTests
{
    public delegate Task<int> RetryRunner(Func<int> action, int maxRetries, Action<string>? logger, Func<TimeSpan, CancellationToken, Task> delay);

    private static readonly RetryRunner Sync = (action, maxRetries, logger, delay) =>
        Task.FromResult(GoogleRetryPolicy.ExecuteWithRetry(action, maxRetries, logger, delay));

    private static readonly RetryRunner Async = (action, maxRetries, logger, delay) =>
        GoogleRetryPolicy.ExecuteWithRetryAsync(() => Task.FromResult(action()), maxRetries, logger, delay);

    public sealed record NamedRunner(string Mode, RetryRunner Run)
    {
        public override string ToString() => Mode;
    }

    public static TheoryData<NamedRunner> Runners() => new() { new NamedRunner("sync", Sync), new NamedRunner("async", Async) };

    private readonly List<TimeSpan> _requestedDelays = [];

    private Task RecordDelay(TimeSpan delay, CancellationToken cancellationToken)
    {
        _requestedDelays.Add(delay);
        return Task.CompletedTask;
    }

    private static GoogleApiException RateLimitedException() =>
        new("sheets", "Rate limited") { HttpStatusCode = HttpStatusCode.TooManyRequests };

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_ActionSucceedsImmediately_ReturnsResultWithoutRetrying(NamedRunner runner)
    {
        var callCount = 0;

        var result = await runner.Run(() =>
        {
            callCount++;
            return 42;
        }, 5, null, RecordDelay);

        result.Should().Be(42);
        callCount.Should().Be(1);
        _requestedDelays.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_RateLimitedOnce_RetriesAndReturnsResult_InvokingLogger(NamedRunner runner)
    {
        var callCount = 0;
        var logMessages = new List<string>();

        var result = await runner.Run(
            () =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw RateLimitedException();
                }
                return 7;
            },
            3,
            logMessages.Add,
            RecordDelay);

        result.Should().Be(7);
        callCount.Should().Be(2);
        logMessages.Should().ContainSingle(m => m.Contains("Retry 1/3"));
        _requestedDelays.Should().Equal(TimeSpan.FromSeconds(2));
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_RateLimitedRepeatedly_BacksOffExponentially(NamedRunner runner)
    {
        var callCount = 0;

        var result = await runner.Run(
            () =>
            {
                callCount++;
                if (callCount < 4)
                {
                    throw RateLimitedException();
                }
                return 9;
            },
            5,
            null,
            RecordDelay);

        result.Should().Be(9);
        _requestedDelays.Should().Equal(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8));
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_ExceedsMaxRetries_ThrowsHttpRequestExceptionWrappingOriginal(NamedRunner runner)
    {
        var act = async () => await runner.Run(() => throw RateLimitedException(), 0, null, RecordDelay);

        var thrown = await act.Should().ThrowAsync<HttpRequestException>();
        thrown.Which.InnerException.Should().BeOfType<GoogleApiException>();
        _requestedDelays.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_NonRateLimitStatusCode_PropagatesImmediatelyWithoutRetrying(NamedRunner runner)
    {
        var callCount = 0;

        var act = async () => await runner.Run(() =>
        {
            callCount++;
            throw new GoogleApiException("sheets", "Bad request") { HttpStatusCode = HttpStatusCode.BadRequest };
        }, 5, null, RecordDelay);

        await act.Should().ThrowAsync<GoogleApiException>();
        callCount.Should().Be(1);
        _requestedDelays.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_NonGoogleApiException_PropagatesImmediately(NamedRunner runner)
    {
        var act = async () => await runner.Run(() => throw new InvalidOperationException("Unrelated failure"), 5, null, RecordDelay);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidActionSucceedsImmediately_CompletesWithoutRetrying()
    {
        var callCount = 0;

        await GoogleRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        callCount.Should().Be(1);
    }
}
