using System.Net;
using System.Net.Http;
using Financial.Integrations.GoogleCore;
using FluentAssertions;
using Google;

namespace Financial.GoogleIntegrations.Tests;

public class GoogleRetryPolicyTests
{
    public delegate Task<int> RetryRunner(Func<int> action, int maxRetries, Action<string>? logger);

    private static readonly RetryRunner Sync = (action, maxRetries, logger) =>
        Task.FromResult(GoogleRetryPolicy.ExecuteWithRetry(action, maxRetries, logger));

    private static readonly RetryRunner Async = (action, maxRetries, logger) =>
        GoogleRetryPolicy.ExecuteWithRetryAsync(() => Task.FromResult(action()), maxRetries, logger);

    public static TheoryData<string, RetryRunner> Runners() => new() { { "sync", Sync }, { "async", Async } };

    private static GoogleApiException RateLimitedException() =>
        new("sheets", "Rate limited") { HttpStatusCode = HttpStatusCode.TooManyRequests };

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_ActionSucceedsImmediately_ReturnsResultWithoutRetrying(string mode, RetryRunner run)
    {
        var callCount = 0;

        var result = await run(() =>
        {
            callCount++;
            return 42;
        }, 5, null);

        result.Should().Be(42);
        callCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_RateLimitedOnce_RetriesAndReturnsResult_InvokingLogger(string mode, RetryRunner run)
    {
        var callCount = 0;
        var logMessages = new List<string>();

        var result = await run(
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
            logMessages.Add);

        result.Should().Be(7);
        callCount.Should().Be(2);
        logMessages.Should().ContainSingle(m => m.Contains("Retry 1/3"));
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_ExceedsMaxRetries_ThrowsHttpRequestExceptionWrappingOriginal(string mode, RetryRunner run)
    {
        var act = async () => await run(() => throw RateLimitedException(), 0, null);

        var thrown = await act.Should().ThrowAsync<HttpRequestException>();
        thrown.Which.InnerException.Should().BeOfType<GoogleApiException>();
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_NonRateLimitStatusCode_PropagatesImmediatelyWithoutRetrying(string mode, RetryRunner run)
    {
        var callCount = 0;

        var act = async () => await run(() =>
        {
            callCount++;
            throw new GoogleApiException("sheets", "Bad request") { HttpStatusCode = HttpStatusCode.BadRequest };
        }, 5, null);

        await act.Should().ThrowAsync<GoogleApiException>();
        callCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(Runners))]
    public async Task ExecuteWithRetry_NonGoogleApiException_PropagatesImmediately(string mode, RetryRunner run)
    {
        var act = async () => await run(() => throw new InvalidOperationException("Unrelated failure"), 5, null);

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
