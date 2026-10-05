using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;

namespace Financial.App.E2ETests.Infrastructure;

internal sealed class AppSession : IDisposable
{
    private static readonly TimeSpan MainWindowTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(5);

    private readonly string _directory;
    private readonly Application _application;
    private readonly UIA3Automation _automation;

    static AppSession() => OrphanReaper.Reap();

    private AppSession(string directory, Application application, UIA3Automation automation, Window window)
    {
        _directory = directory;
        _application = application;
        _automation = automation;
        Window = window;
    }

    public Window Window { get; }

    public static void Run(string testName, Action<AppSession> test)
    {
        using var session = Launch();
        try
        {
            test(session);
        }
        catch
        {
            session.CaptureFailureArtifacts(testName);
            throw;
        }
    }

    public void Dispose()
    {
        _automation.Dispose();
        KillAndDelete(_application.ProcessId, _directory);
    }

    private static AppSession Launch()
    {
        var directory = Path.Combine(AppPaths.TempRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var investmentData = CopyTestData("data.test.json", directory, "data.json");
        var cashFlowData = CopyTestData("data-cashflow.test.json", directory, "data-cashflow.json");
        var fxRatesData = Path.Combine(directory, "fx-rates.json");

        var exe = AppPaths.ExePath();
        var startInfo = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exe)!,
        };
        SetDataEnvironment(startInfo, investmentData, cashFlowData, fxRatesData);

        var application = Application.Launch(startInfo);
        File.WriteAllText(Path.Combine(directory, OrphanReaper.PidFileName), OrphanReaper.Record(Process.GetProcessById(application.ProcessId)));

        var automation = new UIA3Automation();
        try
        {
            var window = application.GetMainWindow(automation, MainWindowTimeout)
                ?? throw new InvalidOperationException("no window handle");
            return new AppSession(directory, application, automation, window);
        }
        catch (Exception exception)
        {
            var outcome = application.HasExited ? $"exited with code {application.ExitCode}" : "still running";
            automation.Dispose();
            KillAndDelete(application.ProcessId, directory);
            throw new InvalidOperationException($"Financial.App did not show its main window ({outcome}): {exception.Message}", exception);
        }
    }

    private static string CopyTestData(string source, string directory, string target)
    {
        var destination = Path.Combine(directory, target);
        AppPaths.EnsureInsideTempRoot(destination);
        File.Copy(Path.Combine(AppPaths.TestDataDirectory, source), destination);
        return destination;
    }

    private static void SetDataEnvironment(ProcessStartInfo startInfo, string investmentData, string cashFlowData, string fxRatesData)
    {
        AppPaths.EnsureInsideTempRoot(fxRatesData);
        var environment = startInfo.Environment;
        environment["Investment__Repository__Provider"] = "LocalJson";
        environment["Investment__DataJsonFile"] = investmentData;
        environment["CashFlow__Repository__Provider"] = "LocalJson";
        environment["CashFlow__DataJsonFile"] = cashFlowData;
        environment["FxRates__Repository__Provider"] = "LocalJson";
        environment["FxRates__DataJsonFile"] = fxRatesData;
        environment["Observability__Enabled"] = "false";
    }

    private static void KillAndDelete(int processId, string directory)
    {
        OrphanReaper.Kill(processId, expectedStartTicks: null, ExitTimeout);
        OrphanReaper.TryDelete(directory);
    }

    private void CaptureFailureArtifacts(string testName)
    {
        var target = Path.Combine(AppPaths.ArtifactsRoot, testName);
        Directory.CreateDirectory(target);

        try
        {
            Capture.Screen().ToFile(Path.Combine(target, "screen.png"));
        }
        catch (Exception exception) when (exception is ExternalException or Win32Exception or InvalidOperationException)
        {
            File.WriteAllText(Path.Combine(target, "screen-error.txt"), exception.ToString());
        }

        File.WriteAllText(
            Path.Combine(target, "process.txt"),
            _application.HasExited
                ? $"Financial.App exited during the test with code {_application.ExitCode}."
                : "Financial.App was still running when the test failed.");

        var logs = Path.Combine(Path.GetDirectoryName(AppPaths.ExePath())!, "logs");
        var latestLog = Directory.Exists(logs)
            ? Directory.EnumerateFiles(logs, "app-*.log").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
        if (latestLog is not null)
        {
            File.Copy(latestLog, Path.Combine(target, Path.GetFileName(latestLog)), overwrite: true);
        }
    }
}
