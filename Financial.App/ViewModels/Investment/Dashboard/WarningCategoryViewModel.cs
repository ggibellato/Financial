namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class WarningCategoryViewModel : ViewModelBase
{
    private bool _isExpanded;

    public WarningCategoryViewModel(
        DataQualityCategory category,
        string label,
        int count,
        IReadOnlyList<WarningFindingRowViewModel> findings)
    {
        Category = category;
        Label = label;
        Count = count;
        Findings = findings ?? throw new ArgumentNullException(nameof(findings));
    }

    public DataQualityCategory Category { get; }

    public string Label { get; }

    public int Count { get; }

    public IReadOnlyList<WarningFindingRowViewModel> Findings { get; }

    public bool HasFindings => Findings.Count > 0;

    public bool IsCountOnly => Findings.Count == 0;

    public string HeaderText => $"{Label} ({Count})";

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
}
