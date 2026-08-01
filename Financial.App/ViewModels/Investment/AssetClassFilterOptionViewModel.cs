using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels;

public sealed class AssetClassFilterOptionViewModel : ViewModelBase
{
    public AssetClassFilterOptionViewModel(string label, GlobalAssetClass? filter)
    {
        Label = label;
        Filter = filter;
    }

    public string Label { get; }
    public GlobalAssetClass? Filter { get; }
}
