using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Replaces WPF's native 2-state, null-first DataGrid column sort with a 3-state
/// (unsorted -> ascending -> descending -> unsorted), null-last sort, matching Financial.Web's
/// useSortableRows behavior. Applied globally via the DataGrid style in App.xaml; opt a specific
/// grid out with `SortableColumnsBehavior.IsEnabled="False"` (e.g. Reserva's Movements grid).
///
/// A grid can also set `DefaultSortMemberPath`/`DefaultSortDirection` to start already sorted -
/// matching Financial.Web's useSortableRows `defaultSort` parameter - so the column header shows
/// the same active-sort arrow it would after the user clicked it.
/// </summary>
public static class SortableColumnsBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(SortableColumnsBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DefaultSortMemberPathProperty = DependencyProperty.RegisterAttached(
        "DefaultSortMemberPath",
        typeof(string),
        typeof(SortableColumnsBehavior),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DefaultSortDirectionProperty = DependencyProperty.RegisterAttached(
        "DefaultSortDirection",
        typeof(ListSortDirection?),
        typeof(SortableColumnsBehavior),
        new PropertyMetadata(null));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(SortState),
        typeof(SortableColumnsBehavior),
        new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetDefaultSortMemberPath(DependencyObject element, string? value) =>
        element.SetValue(DefaultSortMemberPathProperty, value);

    public static string? GetDefaultSortMemberPath(DependencyObject element) =>
        (string?)element.GetValue(DefaultSortMemberPathProperty);

    public static void SetDefaultSortDirection(DependencyObject element, ListSortDirection? value) =>
        element.SetValue(DefaultSortDirectionProperty, value);

    public static ListSortDirection? GetDefaultSortDirection(DependencyObject element) =>
        (ListSortDirection?)element.GetValue(DefaultSortDirectionProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        if (e.NewValue is true)
        {
            grid.Sorting += OnSorting;
            grid.Loaded += OnLoaded;
            return;
        }

        grid.Sorting -= OnSorting;
        grid.Loaded -= OnLoaded;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        var defaultPath = GetDefaultSortMemberPath(grid);
        if (string.IsNullOrEmpty(defaultPath) || grid.GetValue(StateProperty) is not null)
        {
            return;
        }

        var column = grid.Columns.FirstOrDefault(c => string.Equals(c.SortMemberPath, defaultPath, StringComparison.Ordinal));
        if (column is null || CollectionViewSource.GetDefaultView(grid.ItemsSource) is not ListCollectionView view)
        {
            return;
        }

        var direction = GetDefaultSortDirection(grid) ?? ListSortDirection.Ascending;
        view.CustomSort = new PropertyPathComparer(defaultPath, direction);
        column.SortDirection = direction;
        grid.SetValue(StateProperty, new SortState(defaultPath, direction));
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
