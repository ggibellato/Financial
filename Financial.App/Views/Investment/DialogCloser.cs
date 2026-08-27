using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

/// <summary>
/// Wires a dialog ViewModel's CloseRequested event to the hosting Window's DialogResult/Close,
/// self-unsubscribing once fired - the same 3-line block every *Dialog.xaml.cs repeated.
/// </summary>
internal static class DialogCloser
{
    internal static void Attach(Window window, Action<EventHandler<bool?>> subscribe, Action<EventHandler<bool?>> unsubscribe)
    {
        EventHandler<bool?>? handler = null;
        handler = (_, dialogResult) =>
        {
            unsubscribe(handler!);
            window.DialogResult = dialogResult;
            window.Close();
        };
        subscribe(handler);
    }
}
