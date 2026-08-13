using System.Net.Http;
using System.Net.Sockets;

namespace Financial.Shared.Infrastructure.Resilience;

internal static class TransientRetryPolicy
{
    private const int InitialDelayMs = 2000;

    internal static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 5, Action<string>? logger = null)
    {
        int retryCount = 0;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsRetryable(ex) && retryCount < maxRetries)
            {
                retryCount++;
                var waitTime = CalculateWaitTimeMs(retryCount);
                logger?.Invoke($"Transient failure ({ex.GetType().Name}). Retry {retryCount}/{maxRetries}. Waiting {waitTime}ms...");
                await Task.Delay(waitTime);
            }
        }
    }

    private static bool IsRetryable(Exception ex) => ex switch
    {
        TransientStorageException => true,
        HttpRequestException => true,
        TaskCanceledException => true,
        SocketException => true,
        _ => false
    };

    private static int CalculateWaitTimeMs(int retryCount) => InitialDelayMs * (int)Math.Pow(2, retryCount - 1);
}
