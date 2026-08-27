using System.ComponentModel;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Type-aware value comparison where a null value always sorts last, in both directions.
/// </summary>
public static class NullLastComparer
{
    public static int Compare(object? x, object? y, ListSortDirection direction)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return 1;
        }

        if (y is null)
        {
            return -1;
        }

        var comparison = x is IComparable comparableX && x.GetType() == y.GetType()
            ? comparableX.CompareTo(y)
            : Comparer<object>.Default.Compare(x, y);

        return direction == ListSortDirection.Ascending ? comparison : -comparison;
    }
}
