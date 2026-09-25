using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Scrolls and focuses the realized container for an item in an <see cref="ItemsControl"/>, for a
/// cross-panel deep link that reveals a row the user cannot see yet. Deferred to
/// <see cref="DispatcherPriority.Loaded"/> because the container is only realised once the layout
/// pass following whatever made it visible (a tab switch, an Expander opening) has run - neither
/// BringIntoView nor focus has a data-bindable XAML equivalent.
/// </summary>
public static class ItemFocusHelper
{
    public static void FocusAndScrollIntoView(Dispatcher dispatcher, ItemsControl owner, object item)
    {
        dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (owner.ItemContainerGenerator.ContainerFromItem(item) is not FrameworkElement container)
            {
                return;
            }

            container.BringIntoView();

            if (!container.Focus())
            {
                container.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        });
    }
}
