using System.Net.Http;
using System.Net.Sockets;
using Financial.Shared.Abstractions.Resilience;
using Financial.Shared.Infrastructure.Resilience;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Resilience;

[Trait("Category", "Unit")]
public class TransientRetryPolicyTests
{
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

    public static TheoryData<Exception> TransientFailures() =>
    [
        new TransientStorageException("Drive request failed with a transient status.", new InvalidOperationException()),
        new TaskCanceledException("Request timed out"),
        new HttpRequestException("Connection reset"),
        new SocketException((int)SocketError.ConnectionReset),
    ];

    [Theory]
    [MemberData(nameof(TransientFailures))]
    public async Task ExecuteWithRetryAsync_TransientFailure_RetriesAndEventuallySucceeds(Exception failure)
    {
        var callCount = 0;
        var waits = new List<TimeSpan>();
        var logMessages = new List<string>();

        var result = await TransientRetryPolicy.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw failure;
                }
                return Task.FromResult(7);
            },
            logger: logMessages.Add,
            delay: (wait, _) =>
            {
                waits.Add(wait);
                return Task.CompletedTask;
            });

        result.Should().Be(7);
        callCount.Should().Be(2);
        waits.Should().ContainSingle();
        logMessages.Should().ContainSingle().Which.Should().Contain("Retry 1/5").And.Contain(failure.GetType().Name);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ExceedsMaxRetries_SurfacesOriginalExceptionUnwrapped()
    {
        var callCount = 0;

        var act = async () => await TransientRetryPolicy.ExecuteWithRetryAsync<int>(
            () =>
            {
                callCount++;
                throw new TransientStorageException("Drive request failed with a transient status.", new InvalidOperationException());
            },
            maxRetries: 0);

        await act.Should().ThrowAsync<TransientStorageException>();
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
}
