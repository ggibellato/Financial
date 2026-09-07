using System.Diagnostics;

namespace Financial.Presentation.App.Services;

public sealed class BrowserLauncher : IBrowserLauncher
{
    public void OpenUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
