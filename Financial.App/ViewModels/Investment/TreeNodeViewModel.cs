using Financial.Investment.Application.DTOs;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

public class TreeNodeViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isDropTarget;

    public string DisplayName { get; }

    public TreeNodeType NodeType { get; }

    public IReadOnlyDictionary<string, object> Metadata { get; }

    public ObservableCollection<TreeNodeViewModel> Children { get; }

    public TreeNodeViewModel? Parent { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value) && value)
            {
                OnNodeSelected();
            }
        }
    }

    /// <summary>
    /// The asset's position type, or empty for a node that has none.
    /// </summary>
    /// <remarks>
    /// Bound in place of Metadata[PositionType]. The dictionary indexer throws when the key is
    /// absent, which it is for every broker and portfolio row, and WPF logged the exception on each
    /// one - the icon was hidden by its Visibility binding, but the Foreground binding was
    /// evaluated regardless.
    /// </remarks>
    public string PositionType => GetMetadata<string>(NavigationMetadataKeys.PositionType) ?? string.Empty;

    /// <summary>
    /// Whether a drag is currently over this node and it would accept the drop.
    /// </summary>
    /// <remarks>
    /// Lives here rather than on the TreeViewItem because the tree recycles its containers: a
    /// recycled item would otherwise carry another node's highlight.
    /// </remarks>
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set => SetProperty(ref _isDropTarget, value);
    }

    public event EventHandler<TreeNodeViewModel>? NodeSelected;

    public TreeNodeViewModel(TreeNodeDTO dto, TreeNodeViewModel? parent = null)
    {
        DisplayName = dto.DisplayName;
        NodeType = dto.NodeType;
        Metadata = dto.Metadata;
        Parent = parent;
        Children = new ObservableCollection<TreeNodeViewModel>();

        foreach (var child in dto.Children)
        {
            Children.Add(new TreeNodeViewModel(child, this));
        }
    }

    private void OnNodeSelected()
    {
        NodeSelected?.Invoke(this, this);
        Parent?.OnChildNodeSelected(this);
    }

    private void OnChildNodeSelected(TreeNodeViewModel child)
    {
        NodeSelected?.Invoke(this, child);
        Parent?.OnChildNodeSelected(child);
    }

    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}

