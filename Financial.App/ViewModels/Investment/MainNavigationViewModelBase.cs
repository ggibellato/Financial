using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Views.Investment;
using Financial.Investment.Domain.Exceptions;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

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
    private readonly IAssetMoveService _assetMoveService;
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
        InvestmentScope scope,
        IAssetMoveService assetMoveService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _creditQueryService = creditQueryService ?? throw new ArgumentNullException(nameof(creditQueryService));
        _summaryService = summaryService ?? throw new ArgumentNullException(nameof(summaryService));
        _portfolioAssetSummaryService = portfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryService));
        AssetDetails = assetDetails ?? throw new ArgumentNullException(nameof(assetDetails));
        _assetMoveService = assetMoveService ?? throw new ArgumentNullException(nameof(assetMoveService));
        _scope = scope;
        MoveAssetCommand = new RelayCommand(async () => await MoveSelectedAssetAsync(), CanMoveSelectedAsset);
        InitializeAssetClassFilters();
    }

    public RelayCommand MoveAssetCommand { get; }

    /// <summary>
    /// Moves the selected asset into another portfolio of the same broker, then rebuilds the tree
    /// and reselects the asset so the user can see where it landed.
    /// </summary>
    public async Task MoveSelectedAssetAsync()
    {
        if (!CanMoveSelectedAsset())
        {
            return;
        }

        var assetNode = SelectedNode!;
        var brokerName = BrokerNameOf(assetNode);
        var portfolioName = assetNode.Parent!.GetMetadata<string>("PortfolioName") ?? string.Empty;
        var assetName = assetNode.GetMetadata<string>("AssetName") ?? string.Empty;

        // Only a closed position in Active Investments can be archived, so Historic destinations
        // are offered only then - and never for an asset already in Historic, which never comes
        // back out. The server refuses either way; this keeps the dialog from offering it.
        // The sentinel matters: GetMetadata defaults a missing decimal to 0, which reads as "closed"
        // and would offer archiving for every open position. Absent metadata must fail closed.
        var quantity = assetNode.GetMetadata("Quantity", decimal.MinValue);
        var canArchive = _scope == InvestmentScope.Active && quantity == 0m;

        var dialog = new MoveAssetDialogViewModel(
            brokerName,
            portfolioName,
            assetName,
            PortfolioNamesOf(assetNode.Parent.Parent),
            canArchive ? HistoricPortfolioNamesOf(brokerName) : null,
            canArchive);

        if (!ShowMoveAssetDialog(dialog))
        {
            return;
        }

        // Everything after the dialog is inside the catch. The command is invoked as async void,
        // so anything escaping here takes the process down - and on a Google Drive install the
        // upload inside the save is the likeliest thing to fail, not the domain rules.
        try
        {
            var moved = dialog.ArchiveToHistoric
                ? await _assetMoveService.ArchiveAssetAsync(new ArchiveAssetRequestDTO
                {
                    BrokerName = brokerName,
                    SourcePortfolioName = portfolioName,
                    AssetName = assetName,
                    DestinationPortfolioName = dialog.DestinationPortfolioName
                })
                : await _assetMoveService.MoveAssetAsync(new MoveAssetRequestDTO
                {
                    BrokerName = brokerName,
                    Scope = _scope.ToString(),
                    SourcePortfolioName = portfolioName,
                    AssetName = assetName,
                    DestinationPortfolioName = dialog.DestinationPortfolioName
                });

            await LoadNavigationTreeAsync();

            // An archived asset has left this scope entirely, so there is nothing here to reselect -
            // it is now in the Historic Investments view.
            if (!dialog.ArchiveToHistoric)
            {
                SelectAsset(brokerName, moved.PortfolioName, assetName);
            }
        }
        catch (Exception ex)
        {
            // A domain refusal already reads as a sentence for the user; anything else does not,
            // so it is named rather than shown raw.
            ShowMoveFailed(ex is KeyNotFoundException or ArgumentException or InvestmentRuleViolationException
                ? ex.Message
                : $"The asset could not be moved: {ex.GetType().Name}.");
        }
    }

    /// <summary>Seam for tests, which have no message pump to show a modal on.</summary>
    protected virtual bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel) =>
        new MoveAssetDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow }.ShowDialog() == true;

    /// <summary>Seam for tests, for the same reason.</summary>
    protected virtual void ShowMoveFailed(string message) =>
        System.Windows.MessageBox.Show(message, "Move Asset", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

    private bool CanMoveSelectedAsset() =>
        SelectedNode?.NodeType == TreeNodeType.Asset && SelectedNode.Parent?.Parent is not null;

    private static string BrokerNameOf(TreeNodeViewModel assetNode) =>
        assetNode.Parent?.Parent?.GetMetadata<string>("BrokerName") ?? string.Empty;

    /// <summary>
    /// The broker's Historic portfolios, read from the navigation service rather than the tree -
    /// this view model only ever holds one scope's tree, and archiving needs the other one.
    /// </summary>
    private IEnumerable<string> HistoricPortfolioNamesOf(string brokerName) =>
        _navigationService.GetBrokers(InvestmentScope.Historic)
            .Where(broker => broker.Name == brokerName)
            .SelectMany(broker => broker.Portfolios)
            .Select(portfolio => portfolio.Name);

    private static IEnumerable<string> PortfolioNamesOf(TreeNodeViewModel? brokerNode) =>
        brokerNode?.Children
            .Where(child => child.NodeType == TreeNodeType.Portfolio)
            .Select(child => child.GetMetadata<string>("PortfolioName") ?? string.Empty)
            .Where(name => name.Length > 0)
        ?? [];

    /// <summary>
    /// Reselects an asset after the tree has been rebuilt. The rebuild replaces every node, so the
    /// node the user had selected no longer exists and has to be found again by name.
    /// </summary>
    private void SelectAsset(string brokerName, string portfolioName, string assetName)
    {
        var broker = RootNodes.FirstOrDefault(node => node.GetMetadata<string>("BrokerName") == brokerName);
        var portfolio = broker?.Children.FirstOrDefault(node => node.GetMetadata<string>("PortfolioName") == portfolioName);
        var asset = portfolio?.Children.FirstOrDefault(node => node.GetMetadata<string>("AssetName") == assetName);

        if (asset is null)
        {
            return;
        }

        broker!.IsExpanded = true;
        portfolio!.IsExpanded = true;

        // IsSelected, not SelectedNode: the container style binds it two-way, so this is what
        // actually highlights the row in the tree. It raises NodeSelected, which sets SelectedNode
        // on the way through - assigning SelectedNode alone would leave the moved asset current in
        // the view model but unhighlighted in the tree the user is looking at.
        asset.IsSelected = true;
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

        // RootNodes is bound to the tree, so the update has to reach the UI thread - this runs on a
        // worker via LoadNavigationTreeAsync's Task.Run. Outside a running WPF application there is
        // no bound UI thread to marshal to at all, which is also true of the non-UI hosting this
        // class supports, so the update runs here instead.
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            UpdateRootNodes(filteredTree);
            return;
        }

        dispatcher.Invoke(() => UpdateRootNodes(filteredTree));
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

    /// <summary>
    /// A click in the tree arrives here, via TreeViewItem.IsSelected -> TreeNodeViewModel.IsSelected
    /// -> NodeSelected. Assigning the property rather than loading the details directly is what
    /// keeps <see cref="SelectedNode"/> in step with the tree; its setter does the loading. This
    /// used to call LoadSelectionDetails on its own, which left SelectedNode permanently null -
    /// harmless while nothing read it, and the reason the move command could never enable.
    /// </summary>
    private void OnTreeNodeSelected(object? sender, TreeNodeViewModel selectedNode)
    {
        SelectedNode = selectedNode;
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
        var credits = _creditQueryService.GetCreditsByPortfolio(brokerName, portfolioName, _scope);
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
        var credits = _creditQueryService.GetCreditsByBroker(brokerName, _scope);
        AssetDetails.LoadBrokerSummary(brokerName, summary, credits);
        _ = AssetDetails.LoadBrokerBreakdown(brokerName);
        _ = AssetDetails.LoadBrokerTransactions(brokerName);
    }
}
