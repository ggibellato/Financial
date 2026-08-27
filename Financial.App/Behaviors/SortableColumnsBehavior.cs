using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Replaces WPF's native 2-state, null-first DataGrid column sort with a 3-state
/// (unsorted -> ascending -> descending -> unsorted), null-last sort, matching Financial.Web's
/// useSortableRows behavior. Applied globally via the DataGrid style in App.xaml; opt a specific
/// grid out with `SortableColumnsBehavior.IsEnabled="False"` (e.g. Reserva's Movements grid).
/// </summary>
public static class SortableColumnsBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(SortableColumnsBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(SortState),
        typeof(SortableColumnsBehavior),
        new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        if (e.NewValue is true)
        {
            grid.Sorting += OnSorting;
            return;
        }

        grid.Sorting -= OnSorting;
    }

    private static void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        e.Handled = true;

        var sortMemberPath = e.Column.SortMemberPath;
        if (string.IsNullOrEmpty(sortMemberPath))
        {
            return;
        }

        if (CollectionViewSource.GetDefaultView(grid.ItemsSource) is not ListCollectionView view)
        {
            return;
        }

        var currentState = (SortState?)grid.GetValue(StateProperty);
        var nextDirection = SortCycle.Next(currentState?.ColumnPath, currentState?.Direction, sortMemberPath);

        foreach (var column in grid.Columns)
        {
            column.SortDirection = null;
        }

        if (nextDirection is null)
        {
            view.CustomSort = null;
            grid.SetValue(StateProperty, null);
            return;
        }

        view.CustomSort = new PropertyPathComparer(sortMemberPath, nextDirection.Value);
        e.Column.SortDirection = nextDirection.Value;
        grid.SetValue(StateProperty, new SortState(sortMemberPath, nextDirection.Value));
    }

    private sealed record SortState(string ColumnPath, ListSortDirection Direction);

    private sealed class PropertyPathComparer(string propertyPath, ListSortDirection direction) : IComparer
    {
        public int Compare(object? x, object? y) =>
            NullLastComparer.Compare(ResolvePath(x, propertyPath), ResolvePath(y, propertyPath), direction);

        private static object? ResolvePath(object? source, string path)
        {
            foreach (var segment in path.Split('.'))
            {
                if (source is null)
                {
                    return null;
                }

                source = source.GetType().GetProperty(segment)?.GetValue(source);
            }

            return source;
        }
    }
}
