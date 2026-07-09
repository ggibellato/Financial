using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels;

public sealed class TransactionsFilterOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public TransactionsFilterOptionViewModel(string label, PeriodFilter filter)
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
