#nullable enable
using Google;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Financial.Shared.Abstractions.Resilience;

namespace Financial.Integrations.GoogleCore;

public static class GoogleRetryPolicy
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action, int maxRetries = 5, Action<string>? logger = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        try
        {
            return await RetryPolicy.ExecuteWithRetryAsync(action, IsRetryable, maxRetries, logger, delay);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException(
                $"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
        }
    }

    public static async Task ExecuteWithRetryAsync(
        Func<Task> action, int maxRetries = 5, Action<string>? logger = null, Func<TimeSpan, CancellationToken, Task>? delay = null) =>
        await ExecuteWithRetryAsync(
            async () => { await action().ConfigureAwait(false); return true; },
            maxRetries,
            logger,
            delay).ConfigureAwait(false);

    public static T ExecuteWithRetry<T>(
        Func<T> action, int maxRetries = 5, Action<string>? logger = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        try
        {
            return RetryPolicy.ExecuteWithRetry(action, IsRetryable, maxRetries, logger, delay);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException(
                $"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
        }
    }

    private static bool IsRetryable(Exception ex) => ex is GoogleApiException { HttpStatusCode: HttpStatusCode.TooManyRequests };
}
