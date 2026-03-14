using System.Collections.ObjectModel;
using Financial.Application.Interfaces;

namespace Financial.Presentation.UI.ViewModels;

/// <summary>
/// Main ViewModel for the navigation view, coordinating the tree and detail panels
/// </summary>
public class MainNavigationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IOperationService _operationService;
    private readonly ICreditService _creditService;
    private TreeNodeViewModel? _selectedNode;
    private bool _isLoading;

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();
    public AssetDetailsViewModel AssetDetails { get; }

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

    public MainNavigationViewModel(INavigationService navigationService, IOperationService operationService, ICreditService creditService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
        AssetDetails = new AssetDetailsViewModel(_operationService, _creditService);
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
        // Only load details for Asset nodes
        if (selectedNode.NodeType == "Asset")
        {
            LoadAssetDetails(selectedNode);
        }
        else
        {
            AssetDetails.Clear();
        }
    }

    private void OnNodeSelectionChanged()
    {
        if (SelectedNode?.NodeType == "Asset")
        {
            LoadAssetDetails(SelectedNode);
        }
        else
        {
            AssetDetails.Clear();
        }
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
        }
    }
}


