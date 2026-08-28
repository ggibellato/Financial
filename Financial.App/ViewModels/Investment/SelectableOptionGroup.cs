using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Owns one filter/mode selection dimension bound to a set of toolbar buttons
/// (e.g. period filter, chart type) — consolidates the Options/SelectedValue/
/// IsSelected bookkeeping that was duplicated per dimension across
/// CreditsTabViewModel and TransactionsTabViewModel.
/// </summary>
public sealed class SelectableOptionGroup<T>
{
    private T _selectedValue;

    public SelectableOptionGroup(IEnumerable<(string Label, T Value)> options, T initialValue)
    {
        foreach (var (label, value) in options)
            Options.Add(new SelectableOptionViewModel<T>(label, value));
        _selectedValue = initialValue;
        RefreshSelection();
    }

    public ObservableCollection<SelectableOptionViewModel<T>> Options { get; } = new();

    public T SelectedValue => _selectedValue;

    /// <summary>
    /// Applies the new value and refreshes each option's IsSelected flag.
    /// Returns false (no-op beyond refreshing flags) when the value is
    /// already selected, matching the pre-extraction "same value" branch
    /// callers relied on to skip view-state updates and chart rebuilds.
    /// </summary>
    public bool Set(T value)
    {
        if (EqualityComparer<T>.Default.Equals(_selectedValue, value) && Options.Count > 0)
        {
            RefreshSelection();
            return false;
        }

        _selectedValue = value;
        RefreshSelection();
        return true;
    }

    public static bool TryResolve(object? parameter, out T value)
    {
        switch (parameter)
        {
            case SelectableOptionViewModel<T> option:
                value = option.Value;
                return true;
            case T typed:
                value = typed;
                return true;
            default:
                value = default!;
                return false;
        }
    }

    private void RefreshSelection()
    {
        foreach (var option in Options)
            option.IsSelected = EqualityComparer<T>.Default.Equals(option.Value, _selectedValue);
    }
}
