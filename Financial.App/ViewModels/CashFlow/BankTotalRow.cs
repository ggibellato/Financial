using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>One row of the Banks grid: a bank's balance, round-up total, and expandable history, mirroring useMonthly.ts's BankTotal.</summary>
public sealed class BankTotalRow : ViewModelBase
{
    private bool _isExpanded;

    public required string Bank { get; init; }
    public required decimal Balance { get; init; }
    public required decimal RoundUpTotal { get; init; }

    public ObservableCollection<BankHistoryEntry> History { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
}
