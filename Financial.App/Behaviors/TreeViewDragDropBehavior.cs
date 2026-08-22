using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Investment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Lets an asset be dragged onto another portfolio, or onto its broker, in the navigation tree.
/// </summary>
/// <remarks>
/// Pointer plumbing only. Whether a drop is possible, and what it does, belongs to
/// <see cref="IMainNavigationViewModel"/> - WPF has no MVVM-native drag and drop, so something has
/// to translate mouse events into calls, but the rules are not that something's business.
/// </remarks>
public static class TreeViewDragDropBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(TreeViewDragDropBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static Point _dragOrigin;

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView tree)
        {
            return;
        }

        if (e.NewValue is true)
        {
            tree.AllowDrop = true;
            tree.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            tree.PreviewMouseMove += OnPreviewMouseMove;
            tree.DragOver += OnDragOver;
            tree.DragLeave += OnDragLeave;
            tree.Drop += OnDrop;
            return;
        }

        tree.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        tree.PreviewMouseMove -= OnPreviewMouseMove;
        tree.DragOver -= OnDragOver;
        tree.DragLeave -= OnDragLeave;
        tree.Drop -= OnDrop;
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _dragOrigin = e.GetPosition(null);

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not TreeView tree || e.LeftButton != MouseButtonState.Pressed || !PastDragThreshold(e))
        {
            return;
        }

        if (NodeUnder(e.OriginalSource) is { NodeType: TreeNodeType.Asset } asset)
        {
            DragDrop.DoDragDrop(tree, asset, DragDropEffects.Move);
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        var (viewModel, dragged, target) = Resolve(sender, e);
        var accepted = viewModel?.CanAcceptDrop(dragged, target) == true;

        // Highlight only what would actually take the asset, so an illegal drop is visibly
        // impossible rather than merely refused afterwards.
        viewModel?.HighlightDropTarget(accepted ? target : null);

        e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDragLeave(object sender, DragEventArgs e) =>
        ViewModelOf(sender)?.HighlightDropTarget(null);

    private static async void OnDrop(object sender, DragEventArgs e)
    {
        var (viewModel, dragged, target) = Resolve(sender, e);
        e.Handled = true;

        if (viewModel is null)
        {
            return;
        }

        // Releasing on something that cannot take it is a silent cancel; DropAssetAsync clears the
        // highlight either way.
        await viewModel.DropAssetAsync(dragged, target);
    }

    private static (IMainNavigationViewModel? ViewModel, TreeNodeViewModel? Dragged, TreeNodeViewModel? Target) Resolve(
        object sender,
        DragEventArgs e) =>
        (ViewModelOf(sender), DraggedNode(e), NodeUnder(e.OriginalSource));

    private static IMainNavigationViewModel? ViewModelOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as IMainNavigationViewModel;

    private static TreeNodeViewModel? DraggedNode(DragEventArgs e) =>
        e.Data.GetDataPresent(typeof(TreeNodeViewModel))
            ? e.Data.GetData(typeof(TreeNodeViewModel)) as TreeNodeViewModel
            : null;

    /// <summary>The node whose row the pointer is over, found by walking up from whatever was hit.</summary>
    private static TreeNodeViewModel? NodeUnder(object? source)
    {
        var current = source as DependencyObject;
        while (current is not null and not TreeViewItem)
        {
            // GetParent throws on anything that is not a Visual, which the original source of a
            // drag event occasionally is.
            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : null;
        }

        return (current as TreeViewItem)?.DataContext as TreeNodeViewModel;
    }

    private static bool PastDragThreshold(MouseEventArgs e)
    {
        var moved = _dragOrigin - e.GetPosition(null);
        return Math.Abs(moved.X) > SystemParameters.MinimumHorizontalDragDistance
            || Math.Abs(moved.Y) > SystemParameters.MinimumVerticalDragDistance;
    }
}
