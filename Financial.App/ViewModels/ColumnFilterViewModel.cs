using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Financial.Presentation.App.ViewModels;

/// <summary>One selectable value in a column's filter checklist.</summary>
public sealed class FilterValueOption : ViewModelBase
{
    private bool _isChecked;

    public FilterValueOption(string value, bool isChecked)
    {
        Value = value;
        _isChecked = isChecked;
    }

    public string Value { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
}

/// <summary>
/// Non-generic base so a single implicit DataTemplate (registered in App.xaml) can render any
/// DataGridColumn whose Header is assigned one of these — see FilterableColumnHeader.
/// </summary>
public abstract class ColumnFilterViewModelBase : ViewModelBase
{
    private const int SearchBoxThreshold = 10;

    protected ColumnFilterViewModelBase(string label)
    {
        Label = label;
        Options.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowSearch));
    }

    public string Label { get; }

    public ObservableCollection<FilterValueOption> Options { get; } = [];

    public bool IsAllChecked => Options.Count > 0 && Options.All(o => o.IsChecked);

    public bool IsFiltered => Options.Any(o => !o.IsChecked);

    /// <summary>Whether the popup's search box should be shown - past the same 10-value threshold as F03's Web equivalent.</summary>
    public bool ShowSearch => Options.Count > SearchBoxThreshold;

    public abstract ICommand ToggleValueCommand { get; }

    public abstract ICommand ToggleAllCommand { get; }
}

/// <summary>
/// Per-column checklist filter: available values are always computed from the full unfiltered
/// row set passed to <see cref="Refresh"/> - a value never disappears just because another
/// column's filter currently hides its rows. <see cref="Matches"/> returns true if ANY of a
/// row's accessor values is checked, so one row can carry more than one value for the column
/// (e.g. a transfer touching both a source and a destination bank).
/// </summary>
public sealed class ColumnFilterViewModel<T> : ColumnFilterViewModelBase
{
    private readonly Func<T, IEnumerable<string?>> _accessor;
    private readonly Action _onChanged;

    public ColumnFilterViewModel(string label, Func<T, IEnumerable<string?>> accessor, Action onChanged)
        : base(label)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        ToggleValueCommand = new RelayCommand<FilterValueOption>(ToggleValue);
        ToggleAllCommand = new RelayCommand(ToggleAll);
    }

    public override ICommand ToggleValueCommand { get; }

    public override ICommand ToggleAllCommand { get; }

    /// <summary>Recomputes Options from the full unfiltered row set, preserving each value's
    /// checked state where it still exists (a newly-seen value defaults to checked).</summary>
    public void Refresh(IEnumerable<T> rows)
    {
        var previouslyChecked = Options.ToDictionary(o => o.Value, o => o.IsChecked, StringComparer.OrdinalIgnoreCase);

        foreach (var option in Options)
        {
            option.PropertyChanged -= OnOptionPropertyChanged;
        }
        Options.Clear();

        var distinctValues = rows
            .SelectMany(_accessor)
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase);

        foreach (var value in distinctValues)
        {
            var isChecked = !previouslyChecked.TryGetValue(value, out var wasChecked) || wasChecked;
            var option = new FilterValueOption(value, isChecked);
            option.PropertyChanged += OnOptionPropertyChanged;
            Options.Add(option);
        }

        OnPropertyChanged(nameof(IsAllChecked));
        OnPropertyChanged(nameof(IsFiltered));
    }

    public bool Matches(T row)
    {
        if (!IsFiltered)
        {
            return true;
        }

        var checkedValues = new HashSet<string>(
            Options.Where(o => o.IsChecked).Select(o => o.Value),
            StringComparer.OrdinalIgnoreCase);

        return _accessor(row).Any(v => !string.IsNullOrEmpty(v) && checkedValues.Contains(v));
    }

    private void OnOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FilterValueOption.IsChecked))
        {
            return;
        }

        OnPropertyChanged(nameof(IsAllChecked));
        OnPropertyChanged(nameof(IsFiltered));
        _onChanged();
    }

    private void ToggleValue(FilterValueOption? option)
    {
        if (option is null)
        {
            return;
        }

        option.IsChecked = !option.IsChecked;
    }

    private void ToggleAll()
    {
        var makeChecked = !IsAllChecked;
        foreach (var option in Options)
        {
            option.IsChecked = makeChecked;
        }
    }
}
