namespace Financial.Presentation.App.Services;

/// <summary>
/// Wraps launching the OS default browser - a ViewModel would otherwise need a compiled
/// dependency on <see cref="System.Diagnostics.Process"/>, matching the same reasoning
/// <see cref="IDialogService"/> already applies to MessageBox/modal Window calls.
/// </summary>
public interface IBrowserLauncher
{
    void OpenUrl(string url);
}
