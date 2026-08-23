#nullable enable
using Google;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleFinancialSupport;

internal static class GoogleRetryPolicy
{
    private const int InitialDelayMs = 2000;

    internal static Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 5, Action<string>? logger = null) =>
        ExecuteWithRetryCoreAsync(action, maxRetries, logger, Task.Delay);

    internal static T ExecuteWithRetry<T>(Func<T> action, int maxRetries = 5, Action<string>? logger = null) =>
        ExecuteWithRetryCoreAsync(() => Task.FromResult(action()), maxRetries, logger, SleepAsync)
            .GetAwaiter().GetResult();

    private static async Task<T> ExecuteWithRetryCoreAsync<T>(
        Func<Task<T>> action, int maxRetries, Action<string>? logger, Func<int, Task> wait)
    {
        int retryCount = 0;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests && retryCount < maxRetries)
            {
                retryCount++;
                var waitTime = CalculateWaitTimeMs(retryCount);
                logger?.Invoke($"Rate limit hit. Retry {retryCount}/{maxRetries}. Waiting {waitTime}ms...");
                await wait(waitTime);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException(
                    $"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
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
