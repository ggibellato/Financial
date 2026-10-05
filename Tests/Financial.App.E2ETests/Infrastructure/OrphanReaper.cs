using System.ComponentModel;
using System.Diagnostics;

namespace Financial.App.E2ETests.Infrastructure;

internal static class OrphanReaper
{
    internal const string PidFileName = "app.pid";

    private static readonly TimeSpan MinimumAge = TimeSpan.FromMinutes(10);

    public static void Reap()
    {
        if (!Directory.Exists(AppPaths.TempRoot))
        {
            return;
        }

        var cutoff = TimeProvider.System.GetUtcNow().UtcDateTime - MinimumAge;
        foreach (var directory in Directory.EnumerateDirectories(AppPaths.TempRoot).Where(path => Directory.GetCreationTimeUtc(path) < cutoff))
        {
            KillRecordedProcess(Path.Combine(directory, PidFileName));
            TryDelete(directory);
        }
    }

    public static string Record(Process process) => $"{process.Id}:{process.StartTime.Ticks}";

    internal static void Kill(int processId, long? expectedStartTicks, TimeSpan exitTimeout)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (expectedStartTicks is null || process.StartTime.Ticks == expectedStartTicks)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(exitTimeout);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or Win32Exception)
        {
        }
    }

    internal static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void KillRecordedProcess(string pidFile)
    {
        if (!File.Exists(pidFile))
        {
            return;
        }

        var parts = File.ReadAllText(pidFile).Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var id) && long.TryParse(parts[1], out var ticks))
        {
            Kill(id, ticks, TimeSpan.FromSeconds(5));
        }
    }
}
