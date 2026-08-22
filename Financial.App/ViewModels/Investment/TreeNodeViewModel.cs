using Financial.Investment.Application.DTOs;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// ViewModel for hierarchical tree nodes (Broker, Portfolio, Asset)
/// </summary>
public class TreeNodeViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isDropTarget;

    /// <summary>
    /// Display name for the node
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Type of node (Broker, Portfolio, Asset)
    /// </summary>
    public TreeNodeType NodeType { get; }

    /// <summary>
    /// Metadata associated with the node
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Child nodes
    /// </summary>
    public ObservableCollection<TreeNodeViewModel> Children { get; }

    /// <summary>
    /// Parent node (null for root)
    /// </summary>
    public TreeNodeViewModel? Parent { get; }

    /// <summary>
    /// Whether the node is expanded in the tree
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// Whether the node is currently selected
    /// </summary>
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
    public string PositionType => GetMetadata<string>("PositionType") ?? string.Empty;

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

    /// <summary>
    /// Event fired when this node is selected
    /// </summary>
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
        // Bubble up selection event
        NodeSelected?.Invoke(this, child);
        Parent?.OnChildNodeSelected(child);
    }

    /// <summary>
    /// Gets metadata value by key with default
    /// </summary>
    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}

