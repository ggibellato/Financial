using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Financial.Application.Interfaces;

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

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();
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

    protected MainNavigationViewModelBase(INavigationService navigationService, TAssetDetailsViewModel assetDetails)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        AssetDetails = assetDetails ?? throw new ArgumentNullException(nameof(assetDetails));
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
                var tree = _navigationService.GetNavigationTree();

                // Run UI updates on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RootNodes.Clear();

                    // Create root tree node
                    var rootViewModel = new TreeNodeViewModel(tree);

                    // Subscribe to selection events from all nodes
                    SubscribeToNodeEvents(rootViewModel);

                    // Add broker nodes directly (skip root "Investments" node)
                    foreach (var brokerNode in rootViewModel.Children)
                    {
                        RootNodes.Add(brokerNode);
                    }
                });
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

