namespace Financial.Presentation.App.ViewModels;

public sealed class CreditsTypeModeOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public CreditsTypeModeOptionViewModel(string label, CreditsTypeChartMode mode)
    {
        Label = label;
        Mode = mode;
    }

    public string Label { get; }
    public CreditsTypeChartMode Mode { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
