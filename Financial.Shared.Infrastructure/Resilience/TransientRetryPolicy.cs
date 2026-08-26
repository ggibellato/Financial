using System.Net.Http;
using System.Net.Sockets;
using Financial.Shared.Abstractions.Resilience;

namespace Financial.Shared.Infrastructure.Resilience;

internal static class TransientRetryPolicy
{
    internal static Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 5, Action<string>? logger = null) =>
        RetryPolicy.ExecuteWithRetryAsync(action, IsRetryable, maxRetries, logger);

    private static bool IsRetryable(Exception ex) => ex switch
    {
        TransientStorageException => true,
        HttpRequestException => true,
        TaskCanceledException => true,
        SocketException => true,
        _ => false
    };
}
