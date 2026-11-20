namespace Financial.TestUtilities;

public static class Eventually
{
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);

    public static async Task HoldsAsync(Func<bool> condition)
    {
        using var guard = new CancellationTokenSource(HangGuard);
        using var poll = new PeriodicTimer(PollInterval);
        while (!condition())
        {
            try
            {
                await poll.WaitForNextTickAsync(guard.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The condition never held; the code under test is stuck.");
            }
        }
    }
}
