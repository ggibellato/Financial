namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class WarningFindingRowViewModel
{
    public WarningFindingRowViewModel(WarningHoldingRef holding, string secondaryText)
    {
        Holding = holding ?? throw new ArgumentNullException(nameof(holding));
        SecondaryText = secondaryText;
    }

    public WarningHoldingRef Holding { get; }

    public string AssetName => Holding.AssetName;

    public string SecondaryText { get; }
}
