namespace Financial.TestUtilities;

public static class Eventually
{
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(10);

    public static async Task HoldsAsync(Func<bool> condition)
    {
        var met = await Task.Run(() => SpinWait.SpinUntil(condition, HangGuard)).ConfigureAwait(false);
        if (!met)
        {
            throw new TimeoutException("The condition never held; the code under test is stuck.");
        }
    }
}
