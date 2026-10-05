using System.Diagnostics;

namespace Financial.Presentation.Tests;

internal static class AsyncWait
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    public static async Task UntilAsync(Func<bool> condition)
    {
        var started = Stopwatch.GetTimestamp();

        while (!condition())
        {
            if (Stopwatch.GetElapsedTime(started) > Limit)
            {
                throw new TimeoutException("Condition was not met.");
            }

            await Task.Yield();
        }
    }
}
