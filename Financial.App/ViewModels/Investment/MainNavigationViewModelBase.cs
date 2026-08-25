using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Views.Investment;
using Financial.Investment.Domain.Exceptions;
using System.Collections.ObjectModel;

namespace Financial.Presentation.App.ViewModels.Investment;

public abstract class MainNavigationViewModelBase<TAssetDetailsViewModel> : ViewModelBase, IMainNavigationViewModel
    where TAssetDetailsViewModel : class, IAssetDetailsViewModel
{
    private readonly INavigationService _navigationService;
    private readonly ICreditQueryService _creditQueryService;
    private readonly ISummaryService _summaryService;
    private readonly IPortfolioAssetSummaryService _portfolioAssetSummaryService;
    private readonly IAssetMoveService _assetMoveService;
    private readonly IPortfolioService _portfolioService;
    private readonly InvestmentScope _scope;
    private TreeNodeViewModel? _selectedNode;
    private TreeNodeViewModel? _highlightedDropTarget;
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
        IAssetMoveService assetMoveService,
        IPortfolioService portfolioService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _creditQueryService = creditQueryService ?? throw new ArgumentNullException(nameof(creditQueryService));
        _summaryService = summaryService ?? throw new ArgumentNullException(nameof(summaryService));
        _portfolioAssetSummaryService = portfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryService));
        AssetDetails = assetDetails ?? throw new ArgumentNullException(nameof(assetDetails));
        _assetMoveService = assetMoveService ?? throw new ArgumentNullException(nameof(assetMoveService));
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _scope = scope;
        MoveAssetCommand = new RelayCommand(async () => await MoveSelectedAssetAsync(), CanMoveSelectedAsset);
        DeletePortfolioCommand = new RelayCommand(async () => await DeleteSelectedPortfolioAsync(), CanDeleteSelectedPortfolio);
        InitializeAssetClassFilters();
    }

    public RelayCommand MoveAssetCommand { get; }
    public RelayCommand DeletePortfolioCommand { get; }

    public async Task MoveSelectedAssetAsync()
    {
        if (!CanMoveSelectedAsset())
        {
            return;
        }

        var assetNode = SelectedNode!;
        var brokerName = BrokerNameOf(assetNode);
        var portfolioName = assetNode.Parent!.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) ?? string.Empty;
        var assetName = assetNode.GetMetadata<string>(NavigationMetadataKeys.AssetName) ?? string.Empty;

        // Only a closed position in Active Investments can be archived, so Historic destinations
        // are offered only then - and never for an asset already in Historic, which never comes
        // back out. The server refuses either way; this keeps the dialog from offering it.
        // The sentinel matters: GetMetadata defaults a missing decimal to 0, which reads as "closed"
        // and would offer archiving for every open position. Absent metadata must fail closed.
        var quantity = assetNode.GetMetadata(NavigationMetadataKeys.Quantity, decimal.MinValue);
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

        await MoveAssetAsync(brokerName, portfolioName, assetName, dialog.DestinationPortfolioName, dialog.ArchiveToHistoric);
    }

    /// <summary>
    /// Performs the move and everything that follows it. Shared by the dialog and by a drop, so the
    /// two routes cannot drift apart - which is the whole point of offering both.
    /// </summary>
    private async Task MoveAssetAsync(
        string brokerName,
        string portfolioName,
        string assetName,
        string destinationPortfolioName,
        bool archiveToHistoric)
    {
        // Everything here is inside the catch. Both routes are invoked as async void, so anything
        // escaping takes the process down - and on a Google Drive install the upload inside the
        // save is the likeliest thing to fail, not the domain rules.
        try
        {
            var moved = archiveToHistoric
                ? await _assetMoveService.ArchiveAssetAsync(new ArchiveAssetRequestDTO
                {
                    BrokerName = brokerName,
                    SourcePortfolioName = portfolioName,
                    AssetName = assetName,
                    DestinationPortfolioName = destinationPortfolioName
                })
                : await _assetMoveService.MoveAssetAsync(new MoveAssetRequestDTO
                {
                    BrokerName = brokerName,
                    Scope = _scope.ToString(),
                    SourcePortfolioName = portfolioName,
                    AssetName = assetName,
                    DestinationPortfolioName = destinationPortfolioName
                });

            await LoadNavigationTreeAsync();

            // An archived asset has left this scope entirely, so there is nothing here to reselect -
            // it is now in the Historic Investments view.
            if (!archiveToHistoric)
            {
                SelectAsset(brokerName, moved.PortfolioName, assetName);
            }

            await OfferToDeleteEmptiedPortfolioAsync(brokerName, portfolioName);
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

    /// <summary>
    /// Whether dropping <paramref name="dragged"/> on <paramref name="target"/> would do anything.
    /// </summary>
    /// <remarks>
    /// Narrows what the tree offers; it does not decide the move. A drop that looks fine here can
    /// still be refused by the domain - a destination already holding an asset of that name, for
    /// one - and that refusal is what the user is shown.
    /// <para>
    /// A broker is always a valid target, even the one the asset already sits under: dropping there
    /// means "into a new portfolio here", which is the only route to a portfolio that does not
    /// exist yet.
    /// </para>
    /// </remarks>
    public bool CanAcceptDrop(TreeNodeViewModel? dragged, TreeNodeViewModel? target)
    {
        if (dragged?.NodeType != TreeNodeType.Asset || target is null)
        {
            return false;
        }

        var sourcePortfolio = dragged.Parent;
        var sourceBroker = sourcePortfolio?.Parent;
        if (sourceBroker is null)
        {
            return false;
        }

        return target.NodeType switch
        {
            TreeNodeType.Portfolio => ReferenceEquals(target.Parent, sourceBroker) && !ReferenceEquals(target, sourcePortfolio),
            TreeNodeType.Broker => ReferenceEquals(target, sourceBroker),
            _ => false
        };
    }

    public void HighlightDropTarget(TreeNodeViewModel? target)
    {
        if (ReferenceEquals(_highlightedDropTarget, target))
        {
            return;
        }

        if (_highlightedDropTarget is not null)
        {
            _highlightedDropTarget.IsDropTarget = false;
        }

        _highlightedDropTarget = target;

        if (target is not null)
        {
            target.IsDropTarget = true;
        }
    }

    /// <summary>
    /// Completes a drop: onto a portfolio it moves straight there, onto a broker it asks for a name
    /// for the portfolio to create. Everything after that is the dialog route's path, so a drop and
    /// a dialog cannot drift apart.
    /// </summary>
    public async Task DropAssetAsync(TreeNodeViewModel? dragged, TreeNodeViewModel? target)
    {
        HighlightDropTarget(null);

        if (!CanAcceptDrop(dragged, target))
        {
            return;
        }

        var assetNode = dragged!;
        var brokerName = BrokerNameOf(assetNode);
        var portfolioName = assetNode.Parent!.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) ?? string.Empty;
        var assetName = assetNode.GetMetadata<string>(NavigationMetadataKeys.AssetName) ?? string.Empty;

        string destinationPortfolioName;
        if (target!.NodeType == TreeNodeType.Portfolio)
        {
            destinationPortfolioName = target.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) ?? string.Empty;
        }
        else
        {
            // Dropping on the broker means a portfolio that does not exist yet, so the only thing
            // left to ask is its name. An empty destination list is what puts the dialog there.
            var prompt = new MoveAssetDialogViewModel(brokerName, portfolioName, assetName, []);
            if (!ShowMoveAssetDialog(prompt))
            {
                return;
            }

            destinationPortfolioName = prompt.DestinationPortfolioName;
        }

        await MoveAssetAsync(brokerName, portfolioName, assetName, destinationPortfolioName, archiveToHistoric: false);
    }

    /// <summary>
    /// Deletes the portfolio the user has selected, once it holds nothing.
    /// </summary>
    /// <remarks>
    /// Separate from the post-move offer so a portfolio emptied earlier - or in a previous session -
    /// is no harder to remove than one emptied a second ago.
    /// </remarks>
    public async Task DeleteSelectedPortfolioAsync()
    {
        if (!CanDeleteSelectedPortfolio())
        {
            return;
        }

        var portfolioNode = SelectedNode!;
        var brokerName = portfolioNode.Parent!.GetMetadata<string>(NavigationMetadataKeys.BrokerName) ?? string.Empty;
        var portfolioName = portfolioNode.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) ?? string.Empty;

        if (!ConfirmDeletePortfolio(portfolioName))
        {
            return;
        }

        await DeletePortfolioAsync(brokerName, portfolioName);
    }

    /// <summary>
    /// When a move has just emptied the portfolio it came from, says so and offers to remove it.
    /// Declining leaves it in place; the move that already succeeded is untouched either way.
    /// </summary>
    private async Task OfferToDeleteEmptiedPortfolioAsync(string brokerName, string portfolioName)
    {
        if (!IsPortfolioEmpty(brokerName, portfolioName) || !ConfirmDeleteEmptiedPortfolio(portfolioName))
        {
            return;
        }

        await DeletePortfolioAsync(brokerName, portfolioName);
    }

    private async Task DeletePortfolioAsync(string brokerName, string portfolioName)
    {
        try
        {
            await _portfolioService.DeleteEmptyPortfolioAsync(brokerName, portfolioName, _scope);
        }
        catch (Exception ex)
        {
            ShowMoveFailed(ex is KeyNotFoundException or ArgumentException or InvestmentRuleViolationException
                ? ex.Message
                : $"The portfolio could not be deleted: {ex.GetType().Name}.");
            return;
        }

        // Rebuilding clears the selection, which is also how a deleted portfolio stops being the
        // selected node rather than lingering as a stale one.
        await LoadNavigationTreeAsync();
    }

    /// <summary>
    /// Reads the count from the rebuilt tree rather than tracking it: the reload has already
    /// happened, so the tree is the freshest answer available. The sentinel matters - a default of
    /// zero for missing metadata would offer to delete a portfolio that still holds assets.
    /// </summary>
    private bool IsPortfolioEmpty(string brokerName, string portfolioName) =>
        FindPortfolioNode(brokerName, portfolioName)?.GetMetadata(NavigationMetadataKeys.AssetCount, int.MinValue) == 0;

    private TreeNodeViewModel? FindPortfolioNode(string brokerName, string portfolioName) =>
        RootNodes.FirstOrDefault(node => node.GetMetadata<string>(NavigationMetadataKeys.BrokerName) == brokerName)?
            .Children.FirstOrDefault(node => node.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) == portfolioName);

    private bool CanDeleteSelectedPortfolio() =>
        SelectedNode?.NodeType == TreeNodeType.Portfolio
        && SelectedNode.Parent is not null
        && SelectedNode.GetMetadata(NavigationMetadataKeys.AssetCount, int.MinValue) == 0;

    /// <summary>Seam for tests, which have no message pump to show a modal on.</summary>
    protected virtual bool ConfirmDeleteEmptiedPortfolio(string portfolioName) =>
        System.Windows.MessageBox.Show(
            $"\"{portfolioName}\" is now empty. Delete it?",
            "Move Asset",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;

    /// <summary>Seam for tests, for the same reason.</summary>
    protected virtual bool ConfirmDeletePortfolio(string portfolioName) =>
        System.Windows.MessageBox.Show(
            $"Delete the empty portfolio \"{portfolioName}\"?",
            "Delete Portfolio",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;

    /// <summary>Seam for tests, which have no message pump to show a modal on.</summary>
    protected virtual bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel) =>
        new MoveAssetDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow }.ShowDialog() == true;

    /// <summary>Seam for tests, for the same reason.</summary>
    protected virtual void ShowMoveFailed(string message) =>
        System.Windows.MessageBox.Show(message, "Move Asset", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

    private bool CanMoveSelectedAsset() =>
        SelectedNode?.NodeType == TreeNodeType.Asset && SelectedNode.Parent?.Parent is not null;

    private static string BrokerNameOf(TreeNodeViewModel assetNode) =>
        assetNode.Parent?.Parent?.GetMetadata<string>(NavigationMetadataKeys.BrokerName) ?? string.Empty;

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
            .Select(child => child.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) ?? string.Empty)
            .Where(name => name.Length > 0)
        ?? [];

    /// <summary>
    /// Reselects an asset after the tree has been rebuilt. The rebuild replaces every node, so the
    /// node the user had selected no longer exists and has to be found again by name.
    /// </summary>
    private void SelectAsset(string brokerName, string portfolioName, string assetName)
    {
        var broker = RootNodes.FirstOrDefault(node => node.GetMetadata<string>(NavigationMetadataKeys.BrokerName) == brokerName);
        var portfolio = broker?.Children.FirstOrDefault(node => node.GetMetadata<string>(NavigationMetadataKeys.PortfolioName) == portfolioName);
        var asset = portfolio?.Children.FirstOrDefault(node => node.GetMetadata<string>(NavigationMetadataKeys.AssetName) == assetName);

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
            if (node.Metadata.TryGetValue(NavigationMetadataKeys.GlobalAssetClass, out var value) && value is GlobalAssetClass assetClass)
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
        var assetName = assetNode.GetMetadata<string>(NavigationMetadataKeys.AssetName);

        var portfolioNode = assetNode.Parent;
        var brokerNode = portfolioNode?.Parent;

        if (assetName == null || portfolioNode == null || brokerNode == null)
        {
            return;
        }

        var portfolioName = portfolioNode.GetMetadata<string>(NavigationMetadataKeys.PortfolioName);
        var brokerName = brokerNode.GetMetadata<string>(NavigationMetadataKeys.BrokerName);

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
        var portfolioName = portfolioNode.GetMetadata<string>(NavigationMetadataKeys.PortfolioName);
        var brokerNode = portfolioNode.Parent;

        if (portfolioName == null || brokerNode == null)
        {
            AssetDetails.Clear();
            return;
        }

        var brokerName = brokerNode.GetMetadata<string>(NavigationMetadataKeys.BrokerName);
        if (brokerName == null)
        {
            AssetDetails.Clear();
            return;
        }

        var summary = _summaryService.GetPortfolioSummary(brokerName, portfolioName, _scope);
        var credits = _creditQueryService.GetCreditsByPortfolio(brokerName, portfolioName, _scope);
        var assetItems = _portfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName, _scope);
        AssetDetails.LoadPortfolioSummary(brokerName, portfolioName, summary, credits, assetItems);
        _ = AssetDetails.Transactions.LoadPortfolio(brokerName, portfolioName);
    }

    private void LoadBrokerCredits(TreeNodeViewModel brokerNode)
    {
        var brokerName = brokerNode.GetMetadata<string>(NavigationMetadataKeys.BrokerName);
        if (brokerName == null)
        {
            AssetDetails.Clear();
            return;
        }

        var summary = _summaryService.GetBrokerSummary(brokerName, _scope);
        var credits = _creditQueryService.GetCreditsByBroker(brokerName, _scope);
        AssetDetails.LoadBrokerSummary(brokerName, summary, credits);
        _ = AssetDetails.LoadBrokerBreakdown(brokerName);
        _ = AssetDetails.Transactions.LoadBroker(brokerName);
    }
}
