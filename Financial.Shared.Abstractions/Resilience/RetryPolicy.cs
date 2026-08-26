namespace Financial.Shared.Abstractions.Resilience;

public static class RetryPolicy
{
    private const int InitialDelayMs = 2000;

    public static Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action, Func<Exception, bool> isRetryable, int maxRetries = 5, Action<string>? logger = null) =>
        ExecuteWithRetryCoreAsync(action, isRetryable, maxRetries, logger, ms => Task.Delay(ms));

    public static T ExecuteWithRetry<T>(
        Func<T> action, Func<Exception, bool> isRetryable, int maxRetries = 5, Action<string>? logger = null) =>
        ExecuteWithRetryCoreAsync(() => Task.FromResult(action()), isRetryable, maxRetries, logger, SleepAsync)
            .GetAwaiter().GetResult();

    private static async Task<T> ExecuteWithRetryCoreAsync<T>(
        Func<Task<T>> action, Func<Exception, bool> isRetryable, int maxRetries, Action<string>? logger, Func<int, Task> wait)
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
                await wait(waitTime);
            }
        }
    }

    private static Task SleepAsync(int milliseconds)
    {
        Thread.Sleep(milliseconds);
        return Task.CompletedTask;
    }

    private static int CalculateWaitTimeMs(int retryCount) => InitialDelayMs * (int)Math.Pow(2, retryCount - 1);
}
