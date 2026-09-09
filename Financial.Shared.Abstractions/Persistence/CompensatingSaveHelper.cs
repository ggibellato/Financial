namespace Financial.Shared.Abstractions.Persistence;

public static class CompensatingSaveHelper
{
    /// <summary>
    /// Runs <paramref name="applyAndSave"/>; if it throws, runs <paramref name="rollbackAndSave"/>
    /// before rethrowing the original exception unchanged. The rollback edits the same graph the
    /// apply did, so it runs under the same exclusion. Its ApplyAndSaveAsync call should report no
    /// change (return false) - that keeps the correction in memory only, since the failed write
    /// must not be retried.
    /// </summary>
    public static async Task ApplyWithCompensationAsync(Func<Task<bool>> applyAndSave, Func<Task<bool>> rollbackAndSave)
    {
        try
        {
            await applyAndSave().ConfigureAwait(false);
        }
        catch
        {
            await rollbackAndSave().ConfigureAwait(false);
            throw;
        }
    }
}
