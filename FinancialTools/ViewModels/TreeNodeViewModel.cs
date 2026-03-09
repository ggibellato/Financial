using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Financial.Application.DTO;

namespace SharesDividendCheck.ViewModels;

/// <summary>
/// ViewModel for hierarchical tree nodes (Broker, Portfolio, Asset)
/// </summary>
public class TreeNodeViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    /// <summary>
    /// Display name for the node
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Type of node (Broker, Portfolio, Asset)
    /// </summary>
    public string NodeType { get; }

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
