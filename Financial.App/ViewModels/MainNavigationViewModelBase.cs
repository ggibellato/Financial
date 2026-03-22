using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Shared navigation view model logic for UI and Tools
/// </summary>
public abstract class MainNavigationViewModelBase<TAssetDetailsViewModel> : ViewModelBase
    where TAssetDetailsViewModel : class, IAssetDetailsViewModel
{
    private readonly INavigationService _navigationService;
    private TreeNodeViewModel? _selectedNode;
    private bool _isLoading;
    private TreeNodeDTO? _fullTree;
    private AssetClassFilterOptionViewModel? _selectedAssetClassFilter;

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();
    public ObservableCollection<AssetClassFilterOptionViewModel> AssetClassFilters { get; } = new();
    public TAssetDetailsViewModel AssetDetails { get; }

    public TreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                OnNodeSelectionChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public AssetClassFilterOptionViewModel? SelectedAssetClassFilter
    {
        get => _selectedAssetClassFilter;
        set
        {
            if (SetProperty(ref _selectedAssetClassFilter, value))
            {
                ApplyAssetClassFilter();
            }
        }
    }

    protected MainNavigationViewModelBase(INavigationService navigationService, TAssetDetailsViewModel assetDetails)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        AssetDetails = assetDetails ?? throw new ArgumentNullException(nameof(assetDetails));
        InitializeAssetClassFilters();
    }

    /// <summary>
    /// Loads the navigation tree from the service
    /// </summary>
    public async Task LoadNavigationTreeAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Run(() =>
            {
                _fullTree = _navigationService.GetNavigationTree();
                ApplyAssetClassFilter();
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SubscribeToNodeEvents(TreeNodeViewModel node)
    {
        node.NodeSelected += OnTreeNodeSelected;
        foreach (var child in node.Children)
        {
            SubscribeToNodeEvents(child);
        }
    }

    private void ApplyAssetClassFilter()
    {
        if (_fullTree == null)
        {
            return;
        }

        var filter = SelectedAssetClassFilter?.Filter;
        var filteredTree = filter == null ? _fullTree : FilterTreeNode(_fullTree, filter.Value);

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateRootNodes(filteredTree);
        });
    }

    private static TreeNodeDTO? FilterTreeNode(TreeNodeDTO node, GlobalAssetClass filter)
    {
        if (node.NodeType == "Asset")
        {
            if (node.Metadata.TryGetValue("GlobalAssetClass", out var value) && value is GlobalAssetClass assetClass)
            {
                return assetClass == filter ? node : null;
            }

            return filter == GlobalAssetClass.Unknown ? node : null;
        }

        var filteredChildren = node.Children
            .Select(child => FilterTreeNode(child, filter))
            .Where(child => child != null)
            .Select(child => child!)
            .ToList();

        if (filteredChildren.Count == 0)
        {
            return null;
        }

        return new TreeNodeDTO
        {
            NodeType = node.NodeType,
            DisplayName = node.DisplayName,
            Metadata = node.Metadata,
            Children = filteredChildren
        };
    }

    private void UpdateRootNodes(TreeNodeDTO? tree)
    {
        RootNodes.Clear();
        SelectedNode = null;
        if (tree == null)
        {
            return;
        }

        var rootViewModel = new TreeNodeViewModel(tree);
        SubscribeToNodeEvents(rootViewModel);

        foreach (var brokerNode in rootViewModel.Children)
        {
            RootNodes.Add(brokerNode);
        }
    }

    private void InitializeAssetClassFilters()
    {
        AssetClassFilters.Clear();
        AssetClassFilters.Add(new AssetClassFilterOptionViewModel("All", null));

        foreach (var assetClass in Enum.GetValues<GlobalAssetClass>())
        {
            AssetClassFilters.Add(new AssetClassFilterOptionViewModel(BuildAssetClassLabel(assetClass), assetClass));
        }

        SelectedAssetClassFilter = AssetClassFilters.FirstOrDefault();
    }

    private static string BuildAssetClassLabel(GlobalAssetClass assetClass)
    {
        return assetClass switch
        {
            GlobalAssetClass.RealEstate => "Real Estate",
            _ => assetClass.ToString()
        };
    }

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

    private void OnTreeNodeSelected(object? sender, TreeNodeViewModel selectedNode)
    {
        LoadSelectionDetails(selectedNode);
    }

    private void OnNodeSelectionChanged()
    {
        if (SelectedNode == null)
        {
            AssetDetails.Clear();
            return;
        }

        LoadSelectionDetails(SelectedNode);
    }

    private void LoadSelectionDetails(TreeNodeViewModel selectedNode)
    {
        if (selectedNode.NodeType == "Asset")
        {
            LoadAssetDetails(selectedNode);
            return;
        }

        if (selectedNode.NodeType == "Portfolio")
        {
            LoadPortfolioCredits(selectedNode);
            return;
        }

        if (selectedNode.NodeType == "Broker")
        {
            LoadBrokerCredits(selectedNode);
            return;
        }

        AssetDetails.Clear();
    }

    private void LoadAssetDetails(TreeNodeViewModel assetNode)
    {
        var assetName = assetNode.GetMetadata<string>("AssetName");

        // Find broker and portfolio by traversing up the tree
        var portfolioNode = assetNode.Parent;
        var brokerNode = portfolioNode?.Parent;

        if (assetName == null || portfolioNode == null || brokerNode == null)
        {
            return;
        }

        var portfolioName = portfolioNode.GetMetadata<string>("PortfolioName");
        var brokerName = brokerNode.GetMetadata<string>("BrokerName");

        if (portfolioName == null || brokerName == null)
        {
            return;
        }

        var details = _navigationService.GetAssetDetails(brokerName, portfolioName, assetName);

        if (details != null)
        {
            AssetDetails.LoadAssetDetails(details);
            _ = AssetDetails.EnsureTodayInfoLoadedAsync();
        }
    }

    private void LoadPortfolioCredits(TreeNodeViewModel portfolioNode)
    {
        var portfolioName = portfolioNode.GetMetadata<string>("PortfolioName");
        var brokerNode = portfolioNode.Parent;

        if (portfolioName == null || brokerNode == null)
        {
            AssetDetails.Clear();
            return;
        }

        var brokerName = brokerNode.GetMetadata<string>("BrokerName");
        if (brokerName == null)
        {
            AssetDetails.Clear();
            return;
        }

        var credits = _navigationService.GetCreditsByPortfolio(brokerName, portfolioName);
        AssetDetails.LoadPortfolioCredits(brokerName, portfolioName, credits);
    }

    private void LoadBrokerCredits(TreeNodeViewModel brokerNode)
    {
        var brokerName = brokerNode.GetMetadata<string>("BrokerName");
        if (brokerName == null)
        {
            AssetDetails.Clear();
            return;
        }

        var credits = _navigationService.GetCreditsByBroker(brokerName);
        AssetDetails.LoadBrokerCredits(brokerName, credits);
    }
}

