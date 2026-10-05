using System.Diagnostics;

namespace Financial.App.E2ETests.Infrastructure;

internal static class OrphanReaper
{
    internal const string PidFileName = "app.pid";

    public static void Reap()
    {
        if (!Directory.Exists(AppPaths.TempRoot))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(AppPaths.TempRoot))
        {
            KillRecordedProcess(Path.Combine(directory, PidFileName));
            TryDelete(directory);
        }
    }

    public static string Record(Process process) => $"{process.Id}:{process.StartTime.Ticks}";

    internal static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"E2E temp directory not deleted: {directory}: {exception.Message}");
        }
    }

    private static void KillRecordedProcess(string pidFile)
    {
        if (!File.Exists(pidFile))
        {
            return;
        }

        var parts = File.ReadAllText(pidFile).Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var id) || !long.TryParse(parts[1], out var ticks))
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(id);
            if (process.StartTime.Ticks == ticks)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Recorded E2E process {id} is already gone.");
        }
    }
}
