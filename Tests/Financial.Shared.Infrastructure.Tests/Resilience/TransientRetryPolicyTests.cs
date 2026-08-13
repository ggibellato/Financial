using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Financial.Shared.Infrastructure.Resilience;
using FluentAssertions;
using Google;

namespace Financial.Shared.Infrastructure.Tests.Resilience;

public class TransientRetryPolicyTests
{
    private static GoogleApiException GoogleApiException(HttpStatusCode statusCode) =>
        new("drive", "Drive error") { HttpStatusCode = statusCode };

    [Fact]
    public async Task ExecuteWithRetryAsync_ActionSucceedsImmediately_ReturnsResultWithoutRetrying()
    {
        var callCount = 0;

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RateLimited429_RetriesAndEventuallySucceeds()
    {
        var callCount = 0;
        var logMessages = new List<string>();

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw GoogleApiException(HttpStatusCode.TooManyRequests);
                }
                return Task.FromResult(7);
            },
            logger: logMessages.Add);

        result.Should().Be(7);
        callCount.Should().Be(3);
        logMessages.Should().HaveCount(2);
        logMessages[0].Should().Contain("Retry 1/5");
        logMessages[1].Should().Contain("Retry 2/5");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DriveServerError5xx_RetriesAndEventuallySucceeds()
    {
        var callCount = 0;

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw GoogleApiException(HttpStatusCode.ServiceUnavailable);
            }
            return Task.FromResult(7);
        });

        result.Should().Be(7);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NetworkTimeout_RetriesAndEventuallySucceeds()
    {
        var callCount = 0;

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new TaskCanceledException("Request timed out");
            }
            return Task.FromResult(7);
        });

        result.Should().Be(7);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_HttpRequestException_RetriesAndEventuallySucceeds()
    {
        var callCount = 0;

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("Connection reset");
            }
            return Task.FromResult(7);
        });

        result.Should().Be(7);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SocketException_RetriesAndEventuallySucceeds()
    {
        var callCount = 0;

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new SocketException((int)SocketError.ConnectionReset);
            }
            return Task.FromResult(7);
        });

        result.Should().Be(7);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ExceedsMaxRetries_SurfacesOriginalExceptionUnwrapped()
    {
        var callCount = 0;

        var act = async () => await TransientRetryPolicy.ExecuteWithRetryAsync<int>(
            () =>
            {
                callCount++;
                throw GoogleApiException(HttpStatusCode.TooManyRequests);
            },
            maxRetries: 0);

        var thrown = await act.Should().ThrowAsync<GoogleApiException>();
        thrown.Which.HttpStatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonRetryableException_PropagatesImmediatelyWithoutRetrying()
    {
        var callCount = 0;

        var act = async () => await TransientRetryPolicy.ExecuteWithRetryAsync<int>(() =>
        {
            callCount++;
            throw new InvalidOperationException("Unrelated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonRetryableGoogleApiException_PropagatesImmediatelyWithoutRetrying()
    {
        var callCount = 0;

        var act = async () => await TransientRetryPolicy.ExecuteWithRetryAsync<int>(() =>
        {
            callCount++;
            throw GoogleApiException(HttpStatusCode.BadRequest);
        });

        await act.Should().ThrowAsync<GoogleApiException>();
        callCount.Should().Be(1);
    }
}
