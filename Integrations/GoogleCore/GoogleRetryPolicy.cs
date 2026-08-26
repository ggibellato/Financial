#nullable enable
using Google;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Financial.Shared.Abstractions.Resilience;

namespace Financial.Integrations.GoogleCore;

public static class GoogleRetryPolicy
{
    public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 5, Action<string>? logger = null)
    {
        try
        {
            return await RetryPolicy.ExecuteWithRetryAsync(action, IsRetryable, maxRetries, logger);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException(
                $"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
        }
    }

    public static T ExecuteWithRetry<T>(Func<T> action, int maxRetries = 5, Action<string>? logger = null)
    {
        try
        {
            return RetryPolicy.ExecuteWithRetry(action, IsRetryable, maxRetries, logger);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException(
                $"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
        }
    }

    private static bool IsRetryable(Exception ex) => ex is GoogleApiException { HttpStatusCode: HttpStatusCode.TooManyRequests };
}
