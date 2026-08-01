namespace Financial.Presentation.App.ViewModels;

internal sealed class CreditsMonthTypeTotals
{
    public CreditsMonthTypeTotals(DateTime month, IReadOnlyDictionary<string, decimal> totalsByType)
    {
        Month = month;
        TotalsByType = totalsByType;
    }

    public DateTime Month { get; }
    public IReadOnlyDictionary<string, decimal> TotalsByType { get; }
    public decimal Total => TotalsByType.Values.Sum();
}
