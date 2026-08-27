namespace Financial.Presentation.App.ViewModels.Investment;

public sealed class SelectableOptionViewModel<T> : ViewModelBase
{
    private bool _isSelected;

    public SelectableOptionViewModel(string label, T value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public T Value { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
