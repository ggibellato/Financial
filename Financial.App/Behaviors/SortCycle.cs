using System.ComponentModel;

namespace Financial.Presentation.App.Behaviors;

/// <summary>
/// Pure state transition for a single-column, 3-state sort cycle: unsorted -> ascending ->
/// descending -> unsorted. Requesting a different column always resets to ascending.
/// </summary>
public static class SortCycle
{
    public static ListSortDirection? Next(string? currentColumnPath, ListSortDirection? currentDirection, string requestedColumnPath)
    {
        if (!string.Equals(currentColumnPath, requestedColumnPath, StringComparison.Ordinal))
        {
            return ListSortDirection.Ascending;
        }

        return currentDirection switch
        {
            null => ListSortDirection.Ascending,
            ListSortDirection.Ascending => ListSortDirection.Descending,
            ListSortDirection.Descending => null,
            _ => ListSortDirection.Ascending,
        };
    }
}
