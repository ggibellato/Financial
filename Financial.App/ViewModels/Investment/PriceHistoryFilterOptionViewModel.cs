using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels.Investment;

public sealed class PriceHistoryFilterOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public PriceHistoryFilterOptionViewModel(string label, PeriodFilter filter)
    {
        Label = label;
        Filter = filter;
    }

    public string Label { get; }
    public PeriodFilter Filter { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
