namespace Financial.Shared.Abstractions.Resilience;

public static class RetryPolicy
{
    private const int InitialDelayMs = 2000;

    public static Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action, Func<Exception, bool> isRetryable, int maxRetries = 5, Action<string>? logger = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null) =>
        ExecuteWithRetryCoreAsync(action, isRetryable, maxRetries, logger, delay ?? Task.Delay);

    public static T ExecuteWithRetry<T>(
        Func<T> action, Func<Exception, bool> isRetryable, int maxRetries = 5, Action<string>? logger = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null) =>
        ExecuteWithRetryCoreAsync(() => Task.FromResult(action()), isRetryable, maxRetries, logger, delay ?? Task.Delay)
            .GetAwaiter().GetResult();

    private static async Task<T> ExecuteWithRetryCoreAsync<T>(
        Func<Task<T>> action, Func<Exception, bool> isRetryable, int maxRetries, Action<string>? logger,
        Func<TimeSpan, CancellationToken, Task> delay)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (isRetryable(ex) && retryCount < maxRetries)
            {
                retryCount++;
                var waitTime = CalculateWaitTimeMs(retryCount);
                logger?.Invoke($"Retry {retryCount}/{maxRetries} after {ex.GetType().Name}. Waiting {waitTime}ms...");
                await delay(TimeSpan.FromMilliseconds(waitTime), CancellationToken.None);
            }
        }
    }

    private static int CalculateWaitTimeMs(int retryCount) => InitialDelayMs * (int)Math.Pow(2, retryCount - 1);
}
