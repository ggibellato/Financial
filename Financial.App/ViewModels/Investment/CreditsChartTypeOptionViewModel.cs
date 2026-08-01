namespace Financial.Presentation.App.ViewModels;

public sealed class CreditsChartTypeOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public CreditsChartTypeOptionViewModel(string label, CreditsChartType chartType)
    {
        Label = label;
        ChartType = chartType;
    }

    public string Label { get; }
    public CreditsChartType ChartType { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
