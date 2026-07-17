using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Shared navigation view model logic for UI and Tools
/// </summary>
public abstract class MainNavigationViewModelBase<TAssetDetailsViewModel> : ViewModelBase, IMainNavigationViewModel
    where TAssetDetailsViewModel : class, IAssetDetailsViewModel
{
    private readonly INavigationService _navigationService;
    private readonly ICreditQueryService _creditQueryService;
    private readonly ISummaryService _summaryService;
    private readonly IPortfolioAssetSummaryService _portfolioAssetSummaryService;
    private readonly InvestmentScope _scope;
    private TreeNodeViewModel? _selectedNode;
    private bool _isLoading;
    private TreeNodeDTO? _fullTree;
    private AssetClassFilterOptionViewModel? _selectedAssetClassFilter;

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();
    public ObservableCollection<AssetClassFilterOptionViewModel> AssetClassFilters { get; } = new();
    public TAssetDetailsViewModel AssetDetails { get; }
    IAssetDetailsViewModel IMainNavigationViewModel.AssetDetails => AssetDetails;

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

    protected MainNavigationViewModelBase(
        INavigationService navigationService,
        ICreditQueryService creditQueryService,
        ISummaryService summaryService,
        IPortfolioAssetSummaryService portfolioAssetSummaryService,
        TAssetDetailsViewModel assetDetails,
        InvestmentScope scope = InvestmentScope.Active)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _creditQueryService = creditQueryService ?? throw new ArgumentNullException(nameof(creditQueryService));
        _summaryService = summaryService ?? throw new ArgumentNullException(nameof(summaryService));
        _portfolioAssetSummaryService = portfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryService));
        AssetDetails = assetDetails ?? throw new ArgumentNullException(nameof(assetDetails));
        _scope = scope;
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
                _fullTree = _navigationService.GetNavigationTree(_scope);
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
        if (node.NodeType == TreeNodeType.Asset)
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
        if (selectedNode.NodeType == TreeNodeType.Asset)
        {
            LoadAssetDetails(selectedNode);
            return;
        }

        if (selectedNode.NodeType == TreeNodeType.Portfolio)
        {
            LoadPortfolioCredits(selectedNode);
            return;
        }

        if (selectedNode.NodeType == TreeNodeType.Broker)
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

        var details = _navigationService.GetAssetDetails(brokerName, portfolioName, assetName, _scope);

        if (details != null)
        {
            // No single-asset endpoint carries PortfolioWeight (it's inherently
            // portfolio-relative); reuse the portfolio-list summary and match by name,
            // matching the Web app's equivalent approach for the Historic Summary tab.
            decimal? realizedPortfolioWeight = null;
            if (_scope == InvestmentScope.Historic)
            {
                var assetItems = _portfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName, _scope);
                realizedPortfolioWeight = assetItems.FirstOrDefault(item => item.AssetName == assetName)?.PortfolioWeight;
            }

            AssetDetails.LoadAssetDetails(details, realizedPortfolioWeight);
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

        var summary = _summaryService.GetPortfolioSummary(brokerName, portfolioName, _scope);
        var credits = _creditQueryService.GetCreditsByPortfolio(brokerName, portfolioName);
        var assetItems = _portfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName, _scope);
        AssetDetails.LoadPortfolioSummary(brokerName, portfolioName, summary, credits, assetItems);
        _ = AssetDetails.LoadPortfolioTransactions(brokerName, portfolioName);
    }

    private void LoadBrokerCredits(TreeNodeViewModel brokerNode)
    {
        var brokerName = brokerNode.GetMetadata<string>("BrokerName");
        if (brokerName == null)
        {
            AssetDetails.Clear();
            return;
        }

        var summary = _summaryService.GetBrokerSummary(brokerName, _scope);
        var credits = _creditQueryService.GetCreditsByBroker(brokerName);
        AssetDetails.LoadBrokerSummary(brokerName, summary, credits);
        _ = AssetDetails.LoadBrokerBreakdown(brokerName);
        _ = AssetDetails.LoadBrokerTransactions(brokerName);
    }
}
