using System.Net;
using System.Net.Http;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using FluentAssertions;
using Google;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

public class GoogleRetryPolicyTests
{
    private static GoogleApiException RateLimitedException() =>
        new("sheets", "Rate limited") { HttpStatusCode = HttpStatusCode.TooManyRequests };

    [Fact]
    public async Task ExecuteWithRetryAsync_ActionSucceedsImmediately_ReturnsResultWithoutRetrying()
    {
        var callCount = 0;

        var result = await GoogleRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RateLimitedOnce_RetriesAndReturnsResult_InvokingLogger()
    {
        var callCount = 0;
        var logMessages = new List<string>();

        var result = await GoogleRetryPolicy.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw RateLimitedException();
                }
                return Task.FromResult(7);
            },
            maxRetries: 3,
            logger: logMessages.Add);

        result.Should().Be(7);
        callCount.Should().Be(2);
        logMessages.Should().ContainSingle(m => m.Contains("Retry 1/3"));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ExceedsMaxRetries_ThrowsHttpRequestExceptionWrappingOriginal()
    {
        var act = async () => await GoogleRetryPolicy.ExecuteWithRetryAsync<int>(
            () => throw RateLimitedException(),
            maxRetries: 0);

        var thrown = await act.Should().ThrowAsync<HttpRequestException>();
        thrown.Which.InnerException.Should().BeOfType<GoogleApiException>();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonRateLimitStatusCode_PropagatesImmediatelyWithoutRetrying()
    {
        var callCount = 0;

        var act = async () => await GoogleRetryPolicy.ExecuteWithRetryAsync<int>(() =>
        {
            callCount++;
            throw new GoogleApiException("sheets", "Bad request") { HttpStatusCode = HttpStatusCode.BadRequest };
        });

        await act.Should().ThrowAsync<GoogleApiException>();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonGoogleApiException_PropagatesImmediately()
    {
        var act = async () => await GoogleRetryPolicy.ExecuteWithRetryAsync<int>(
            () => throw new InvalidOperationException("Unrelated failure"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
