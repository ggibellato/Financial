#nullable enable
using Google;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

internal static class GoogleRetryPolicy
{
    internal static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 5, Action<string>? logger = null)
    {
        int retryCount = 0;
        const int initialDelayMs = 2000;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests && retryCount < maxRetries)
            {
                retryCount++;
                var waitTime = initialDelayMs * (int)Math.Pow(2, retryCount - 1);
                logger?.Invoke($"Rate limit hit. Retry {retryCount}/{maxRetries}. Waiting {waitTime}ms...");
                await Task.Delay(waitTime);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException(
                    $"API rate limit exceeded after {maxRetries} retries. Please wait a few minutes and try again.", ex);
            }
        }
    }
}
