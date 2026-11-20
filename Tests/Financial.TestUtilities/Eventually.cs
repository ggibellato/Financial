namespace Financial.TestUtilities;

public static class Eventually
{
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);

    public static Task HoldsAsync(Func<bool> condition) => HoldsAsync(() => Task.FromResult(condition()));

    public static async Task HoldsAsync(Func<Task<bool>> condition)
    {
        using var guard = new CancellationTokenSource(HangGuard);
        using var poll = new PeriodicTimer(PollInterval);
        while (!await condition().ConfigureAwait(false))
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
