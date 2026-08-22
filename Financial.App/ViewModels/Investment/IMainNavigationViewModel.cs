namespace Financial.Presentation.App.ViewModels.Investment;

public interface IMainNavigationViewModel
{
    IAssetDetailsViewModel AssetDetails { get; }

    /// <summary>Whether dropping one node on another would do anything.</summary>
    bool CanAcceptDrop(TreeNodeViewModel? dragged, TreeNodeViewModel? target);

    /// <summary>Highlights the node a drag is over, and only that one. Null clears the highlight.</summary>
    void HighlightDropTarget(TreeNodeViewModel? target);

    /// <summary>Completes a drop. A target that cannot take the asset is a silent cancel.</summary>
    Task DropAssetAsync(TreeNodeViewModel? dragged, TreeNodeViewModel? target);
}
