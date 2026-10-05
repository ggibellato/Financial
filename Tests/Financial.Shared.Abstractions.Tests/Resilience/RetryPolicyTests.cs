using Financial.Shared.Abstractions.Resilience;
using FluentAssertions;

namespace Financial.Shared.Abstractions.Tests.Resilience;

[Trait("Category", "Unit")]
public class RetryPolicyTests
{
    private readonly List<TimeSpan> _delays = [];

    private Task RecordDelay(TimeSpan delay, CancellationToken cancellationToken)
    {
        _delays.Add(delay);
        return Task.CompletedTask;
    }

    private static Func<Task<int>> FailsThenSucceeds(int failures, Exception failure)
    {
        var attempts = 0;
        return () => ++attempts <= failures ? throw failure : Task.FromResult(attempts);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FirstAttemptSucceeds_ReturnsWithoutDelay()
    {
        var result = await RetryPolicy.ExecuteWithRetryAsync(() => Task.FromResult(7), _ => true, delay: RecordDelay);

        result.Should().Be(7);
        _delays.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetryableFailures_BacksOffExponentiallyFromTwoSeconds()
    {
        var result = await RetryPolicy.ExecuteWithRetryAsync(
            FailsThenSucceeds(3, new HttpRequestException("503")), _ => true, delay: RecordDelay);

        result.Should().Be(4);
        _delays.Should().Equal(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesExhausted_RethrowsTheLastFailure()
    {
        var act = () => RetryPolicy.ExecuteWithRetryAsync(
            FailsThenSucceeds(10, new HttpRequestException("503")), _ => true, maxRetries: 2, delay: RecordDelay);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("503");
        _delays.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonRetryableFailure_PropagatesWithoutDelay()
    {
        var act = () => RetryPolicy.ExecuteWithRetryAsync(
            FailsThenSucceeds(1, new ArgumentException("bad")), ex => ex is HttpRequestException, delay: RecordDelay);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("bad");
        _delays.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_LogsEachRetryWithItsCountAndWait()
    {
        var messages = new List<string>();

        await RetryPolicy.ExecuteWithRetryAsync(
            FailsThenSucceeds(1, new HttpRequestException("503")), _ => true, maxRetries: 5, logger: messages.Add, delay: RecordDelay);

        messages.Should().Equal("Retry 1/5 after HttpRequestException. Waiting 2000ms...");
    }

    [Fact]
    public void ExecuteWithRetry_RetryableFailure_RetriesSynchronouslyWithTheSameBackoff()
    {
        var attempts = 0;

        var result = RetryPolicy.ExecuteWithRetry(
            () => ++attempts < 3 ? throw new HttpRequestException("503") : attempts, _ => true, delay: RecordDelay);

        result.Should().Be(3);
        _delays.Should().Equal(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
    }
}
