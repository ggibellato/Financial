namespace Financial.Presentation.App.ViewModels;

public sealed class ChartTypeModeOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public ChartTypeModeOptionViewModel(string label, ChartTypeMode mode)
    {
        Label = label;
        Mode = mode;
    }

    public string Label { get; }
    public ChartTypeMode Mode { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
