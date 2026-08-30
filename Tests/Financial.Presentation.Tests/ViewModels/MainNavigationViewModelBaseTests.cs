using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Services;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using System.Windows;

namespace Financial.Presentation.Tests.ViewModels;

public class MainNavigationViewModelBaseTests
{
    private readonly StubSummaryService _summaryService;
    private readonly SpyAssetDetailsViewModel _spy;
    private readonly StubPortfolioAssetSummaryService _assetSummaryService;
    private readonly StubNavigationService _navigationService;
    private readonly StubCreditQueryService _creditQueryService;
    private readonly TestableNavigationViewModel _sut;

    public MainNavigationViewModelBaseTests()
    {
        _summaryService = new StubSummaryService();
        _spy = new SpyAssetDetailsViewModel();
        _assetSummaryService = new StubPortfolioAssetSummaryService();
        _navigationService = new StubNavigationService();
        _creditQueryService = new StubCreditQueryService();
        _sut = CreateViewModel();
    }

    /// <summary>Builds the view model over the shared stubs. Scope is the only thing these tests vary
    /// at construction time; every collaborator is configured through its field instead.</summary>
    private TestableNavigationViewModel CreateViewModel(InvestmentScope scope = InvestmentScope.Active) =>
        new(_summaryService, _spy, _assetSummaryService, _navigationService, scope, _creditQueryService);

    [Fact]
    public void AssetClassFilters_IncludesCryptocurrencyWithCorrectLabel()
    {
        _sut.AssetClassFilters.Should().ContainSingle(f =>
            f.Filter == GlobalAssetClass.Cryptocurrency && f.Label == "Cryptocurrency");
    }

    [Fact]
    public void SelectingPortfolioNode_LoadsTotalSoldFromSummaryService()
    {
        var summary = new AggregatedSummaryDTO { TotalBought = 10000m, TotalSold = 4706.65m, TotalCredits = 500m };
        _summaryService.PortfolioSummary = summary;

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _spy.LastPortfolioSummary.Should().NotBeNull();
        _spy.LastPortfolioSummary!.TotalSold.Should().Be(4706.65m);
        _spy.LastPortfolioSummary.TotalBought.Should().Be(10000m);
    }

    [Fact]
    public void SelectingBrokerNode_LoadsTotalSoldFromSummaryService()
    {
        var summary = new AggregatedSummaryDTO { TotalBought = 20000m, TotalSold = 9000m, TotalCredits = 800m };
        _summaryService.BrokerSummary = summary;

        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _spy.LastBrokerSummary.Should().NotBeNull();
        _spy.LastBrokerSummary!.TotalSold.Should().Be(9000m);
        _spy.LastBrokerSummary.TotalBought.Should().Be(20000m);
    }

    [Fact]
    public void SelectingPortfolioNode_PassesCorrectBrokerAndPortfolioNameToSummaryService()
    {
        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _summaryService.LastBrokerNameForPortfolio.Should().Be("XPI");
        _summaryService.LastPortfolioName.Should().Be("FII");
    }

    [Fact]
    public void SelectingBrokerNode_PassesCorrectBrokerNameToSummaryService()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _summaryService.LastBrokerNameForBroker.Should().Be("XPI");
    }

    [Fact]
    public void SelectingBrokerNode_CallsLoadBrokerSummaryOnDetailsViewModel()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _spy.WasBrokerSummaryLoaded.Should().BeTrue();
        _spy.LastBrokerSummary.Should().NotBeNull();
    }

    [Fact]
    public void SelectingBrokerNode_CallsLoadBrokerBreakdownOnDetailsViewModel()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _spy.WasBrokerBreakdownLoaded.Should().BeTrue();
        _spy.LastBrokerBreakdownName.Should().Be("XPI");
    }

    [Fact]
    public void SelectingBrokerNode_CallsLoadBrokerTransactionsOnDetailsViewModel()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _spy.TransactionsSpy.WasBrokerTransactionsLoaded.Should().BeTrue();
        _spy.TransactionsSpy.LastBrokerTransactionsName.Should().Be("XPI");
    }

    [Fact]
    public void SelectingPortfolioNode_CallsLoadPortfolioTransactionsOnDetailsViewModel()
    {
        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _spy.TransactionsSpy.WasPortfolioTransactionsLoaded.Should().BeTrue();
        _spy.TransactionsSpy.LastPortfolioTransactionsBrokerName.Should().Be("XPI");
        _spy.TransactionsSpy.LastPortfolioTransactionsPortfolioName.Should().Be("FII");
    }

    [Fact]
    public void SelectingBrokerNode_RequestsActiveScopeForCredits()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _creditQueryService.LastBrokerScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingBrokerNode_HistoricScope_RequestsHistoricScopeForCredits()
    {
        var vm = CreateViewModel(InvestmentScope.Historic);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        _creditQueryService.LastBrokerScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingPortfolioNode_HistoricScope_RequestsHistoricScopeForCredits()
    {
        var vm = CreateViewModel(InvestmentScope.Historic);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        _creditQueryService.LastPortfolioScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingPortfolioNode_WhenMissingMetadata_ClearsDetails()
    {
        var nodeWithoutMetadata = BuildNodeWithoutMetadata(TreeNodeType.Portfolio);
        _sut.SelectedNode = nodeWithoutMetadata;

        _spy.WasCleared.Should().BeTrue();
        _spy.LastPortfolioSummary.Should().BeNull();
    }

    [Fact]
    public void SelectingPortfolioNode_CallsLoadPortfolioSummaryOnDetailsViewModel()
    {
        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _spy.WasPortfolioSummaryLoaded.Should().BeTrue();
        _spy.LastPortfolioSummary.Should().NotBeNull();
    }

    [Fact]
    public void SelectingPortfolioNode_PassesCorrectAssetItemsFromService()
    {
        _assetSummaryService.Items =
        [
            new PortfolioAssetSummaryItemDTO { AssetName = "A", Ticker = "A", Exchange = "LSE", TotalInvested = 100m },
            new PortfolioAssetSummaryItemDTO { AssetName = "B", Ticker = "B", Exchange = "LSE", TotalInvested = 200m }
        ];

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _spy.LastPortfolioAssetItems.Should().HaveCount(2);
    }

    [Fact]
    public void SelectingPortfolioNode_PassesCorrectBrokerAndPortfolioNameToAssetSummaryService()
    {
        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _assetSummaryService.LastBrokerName.Should().Be("XPI");
        _assetSummaryService.LastPortfolioName.Should().Be("FII");
    }

    [Fact]
    public void SelectingAssetNode_DoesNotCallLoadBrokerOrPortfolioTransactions()
    {
        var assetNode = BuildAssetNode("XPI", "Acoes", "BBAS3");
        _sut.SelectedNode = assetNode;

        _spy.TransactionsSpy.WasBrokerTransactionsLoaded.Should().BeFalse();
        _spy.TransactionsSpy.WasPortfolioTransactionsLoaded.Should().BeFalse();
    }

    [Fact]
    public void SelectingBrokerNode_DoesNotCallLoadPortfolioSummary()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _spy.WasPortfolioSummaryLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task LoadNavigationTreeAsync_RequestsActiveScope()
    {
        await _sut.LoadNavigationTreeAsync();

        _navigationService.LastTreeScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingAssetNode_RequestsActiveScopeAssetDetails()
    {
        var assetNode = BuildAssetNode("XPI", "Acoes", "BBAS3");
        _sut.SelectedNode = assetNode;

        _navigationService.LastAssetDetailsScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingPortfolioNode_RequestsActiveScopeSummaryAndAssetItems()
    {
        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        _sut.SelectedNode = portfolioNode;

        _summaryService.LastScopeForPortfolio.Should().Be(InvestmentScope.Active);
        _assetSummaryService.LastScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingBrokerNode_RequestsActiveScopeSummary()
    {
        var brokerNode = BuildBrokerNode("XPI");
        _sut.SelectedNode = brokerNode;

        _summaryService.LastScopeForBroker.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public async Task LoadNavigationTreeAsync_HistoricScope_RequestsHistoricScope()
    {
        var vm = CreateViewModel(InvestmentScope.Historic);

        try
        {
            await vm.LoadNavigationTreeAsync();
        }
        catch (NullReferenceException)
        {
        }

        _navigationService.LastTreeScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingAssetNode_HistoricScope_RequestsHistoricScopeAssetDetails()
    {
        var vm = CreateViewModel(InvestmentScope.Historic);

        var assetNode = BuildAssetNode("XPI", "Uncategorized", "BBAS3");
        vm.SelectedNode = assetNode;

        _navigationService.LastAssetDetailsScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingAssetNode_HistoricScope_PassesMatchingPortfolioWeightToAssetDetails()
    {
        _navigationService.AssetDetails = new AssetDetailsDTO { Name = "BBAS3", BrokerName = "XPI", PortfolioName = "Uncategorized", Ticker = "BBAS3" };
        _assetSummaryService.Items =
        [
            new PortfolioAssetSummaryItemDTO { AssetName = "Other Asset", PortfolioWeight = 40m },
            new PortfolioAssetSummaryItemDTO { AssetName = "BBAS3", PortfolioWeight = 5.15m }
        ];
        var vm = CreateViewModel(InvestmentScope.Historic);

        var assetNode = BuildAssetNode("XPI", "Uncategorized", "BBAS3");
        vm.SelectedNode = assetNode;

        _spy.LastAssetDetails.Should().NotBeNull();
        _spy.LastRealizedPortfolioWeight.Should().Be(5.15m);
        _assetSummaryService.LastBrokerName.Should().Be("XPI");
        _assetSummaryService.LastPortfolioName.Should().Be("Uncategorized");
        _assetSummaryService.LastScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingAssetNode_ActiveScope_DoesNotFetchPortfolioWeight()
    {
        _navigationService.AssetDetails = new AssetDetailsDTO { Name = "BBAS3", BrokerName = "XPI", PortfolioName = "Acoes", Ticker = "BBAS3" };

        var assetNode = BuildAssetNode("XPI", "Acoes", "BBAS3");
        _sut.SelectedNode = assetNode;

        _assetSummaryService.LastBrokerName.Should().BeNull();
        _spy.LastRealizedPortfolioWeight.Should().BeNull();
    }

    [Fact]
    public void SelectingPortfolioNode_HistoricScope_RequestsHistoricScopeSummaryAndAssetItems()
    {
        var vm = CreateViewModel(InvestmentScope.Historic);

        var portfolioNode = BuildPortfolioNode("XPI", "Uncategorized");
        vm.SelectedNode = portfolioNode;

        _summaryService.LastScopeForPortfolio.Should().Be(InvestmentScope.Historic);
        _assetSummaryService.LastScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingBrokerNode_HistoricScope_RequestsHistoricScopeSummary()
    {
        var vm = CreateViewModel(InvestmentScope.Historic);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        _summaryService.LastScopeForBroker.Should().Be(InvestmentScope.Historic);
    }

    private static TreeNodeViewModel BuildPortfolioNode(string brokerName, string portfolioName)
    {
        var brokerDto = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Broker,
            DisplayName = brokerName,
            Metadata = new Dictionary<string, object> { ["BrokerName"] = brokerName },
            Children = []
        };

        var portfolioDto = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Portfolio,
            DisplayName = portfolioName,
            Metadata = new Dictionary<string, object> { ["PortfolioName"] = portfolioName },
            Children = []
        };

        var brokerNode = new TreeNodeViewModel(brokerDto);
        var portfolioNode = new TreeNodeViewModel(portfolioDto, brokerNode);
        return portfolioNode;
    }

    private static TreeNodeViewModel BuildBrokerNode(string brokerName)
    {
        var brokerDto = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Broker,
            DisplayName = brokerName,
            Metadata = new Dictionary<string, object> { ["BrokerName"] = brokerName },
            Children = []
        };
        return new TreeNodeViewModel(brokerDto);
    }

    private static TreeNodeViewModel BuildAssetNode(string brokerName, string portfolioName, string assetName)
    {
        var brokerDto = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Broker,
            DisplayName = brokerName,
            Metadata = new Dictionary<string, object> { ["BrokerName"] = brokerName },
            Children = []
        };
        var portfolioDto = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Portfolio,
            DisplayName = portfolioName,
            Metadata = new Dictionary<string, object> { ["PortfolioName"] = portfolioName },
            Children = []
        };
        var assetDto = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Asset,
            DisplayName = assetName,
            Metadata = new Dictionary<string, object> { ["AssetName"] = assetName },
            Children = []
        };

        var brokerNode = new TreeNodeViewModel(brokerDto);
        var portfolioNode = new TreeNodeViewModel(portfolioDto, brokerNode);
        return new TreeNodeViewModel(assetDto, portfolioNode);
    }

    private static TreeNodeViewModel BuildNodeWithoutMetadata(TreeNodeType nodeType)
    {
        var dto = new TreeNodeDTO
        {
            NodeType = nodeType,
            DisplayName = "Unknown",
            Metadata = new Dictionary<string, object>(),
            Children = []
        };
        return new TreeNodeViewModel(dto);
    }

    internal sealed class TestableNavigationViewModel : MainNavigationViewModelBase<SpyAssetDetailsViewModel>
    {
        public StubNavigationService NavigationService { get; }

        public TestableNavigationViewModel(
            ISummaryService summaryService,
            SpyAssetDetailsViewModel spy,
            IPortfolioAssetSummaryService? portfolioAssetSummaryService = null,
            StubNavigationService? _navigationService = null,
            InvestmentScope scope = InvestmentScope.Active,
            ICreditQueryService? _creditQueryService = null,
            IAssetMoveService? assetMoveService = null,
            IPortfolioService? portfolioService = null)
            : this(_navigationService ?? new StubNavigationService(), summaryService, spy, portfolioAssetSummaryService, scope, _creditQueryService, assetMoveService ?? new StubAssetMoveService(), portfolioService ?? new StubPortfolioService())
        {
        }

        private TestableNavigationViewModel(
            StubNavigationService _navigationService,
            ISummaryService summaryService,
            SpyAssetDetailsViewModel spy,
            IPortfolioAssetSummaryService? portfolioAssetSummaryService,
            InvestmentScope scope,
            ICreditQueryService? _creditQueryService,
            IAssetMoveService assetMoveService,
            IPortfolioService portfolioService)
            : base(_navigationService, _creditQueryService ?? new StubCreditQueryService(), summaryService, portfolioAssetSummaryService ?? new StubPortfolioAssetSummaryService(), spy, scope, assetMoveService, portfolioService, new StubDialogService())
        {
            NavigationService = _navigationService;
        }

        /// <summary>Stands in for the modal, which has no message pump in a test host.</summary>
        public Func<MoveAssetDialogViewModel, bool> MoveDialogResponse { get; set; } = _ => false;

        public MoveAssetDialogViewModel? LastMoveDialog { get; private set; }

        public string? LastMoveFailureMessage { get; private set; }

        protected override bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel)
        {
            LastMoveDialog = viewModel;
            return MoveDialogResponse(viewModel);
        }

        protected override void ShowMoveFailed(string message) => LastMoveFailureMessage = message;

        /// <summary>Stands in for the Yes/No prompt, which has no message pump in a test host.</summary>
        public Func<string, bool> DeleteConfirmationResponse { get; set; } = _ => false;

        public string? LastEmptiedPortfolioOffered { get; private set; }

        protected override bool ConfirmDeleteEmptiedPortfolio(string portfolioName)
        {
            LastEmptiedPortfolioOffered = portfolioName;
            return DeleteConfirmationResponse(portfolioName);
        }

        protected override bool ConfirmDeletePortfolio(string portfolioName) => DeleteConfirmationResponse(portfolioName);

        /// <summary>Stands in for the service in the many base tests that never move anything.</summary>
        private sealed class StubAssetMoveService : IAssetMoveService
        {
            public Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request) => throw new NotImplementedException();
            public Task<AssetDetailsDTO> ArchiveAssetAsync(ArchiveAssetRequestDTO request) => throw new NotImplementedException();
        }

        /// <summary>Stands in for the service in the many base tests that never delete anything.</summary>
        private sealed class StubPortfolioService : IPortfolioService
        {
            public IReadOnlyList<PortfolioDTO> GetPortfolios() => throw new NotImplementedException();
            public Task<PortfolioDTO> CreatePortfolioAsync(PortfolioCreateDTO request) => throw new NotImplementedException();
            public Task<PortfolioDTO> UpdatePortfolioAsync(string brokerName, string currentName, PortfolioUpdateDTO request) =>
                throw new NotImplementedException();
            public Task DeleteEmptyPortfolioAsync(string brokerName, string portfolioName, InvestmentScope scope) =>
                throw new NotImplementedException();
        }

        /// <summary>Never invoked: every seam that would call it is overridden above.</summary>
        private sealed class StubDialogService : IDialogService
        {
            public bool Confirm(string message, string caption) => throw new NotImplementedException();
            public void ShowWarning(string message, string caption) => throw new NotImplementedException();
            public bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel) => throw new NotImplementedException();
            public bool ShowBrokerFormDialog(Financial.Presentation.App.ViewModels.Admin.BrokerFormDialogViewModel viewModel) =>
                throw new NotImplementedException();
        }
    }

    internal sealed class SpyAssetDetailsViewModel : IAssetDetailsViewModel
    {
        public AggregatedSummaryDTO? LastPortfolioSummary { get; private set; }
        public AggregatedSummaryDTO? LastBrokerSummary { get; private set; }
        public IReadOnlyList<PortfolioAssetSummaryItemDTO>? LastPortfolioAssetItems { get; private set; }
        public bool WasCleared { get; private set; }
        public bool WasPortfolioSummaryLoaded { get; private set; }
        public bool WasBrokerSummaryLoaded { get; private set; }
        public string? LastBrokerBreakdownName { get; private set; }
        public bool WasBrokerBreakdownLoaded { get; private set; }
        public bool IsPortfolioView => false;
        public bool IsBrokerView => false;
        public bool IsAssetView => false;
        public int SelectedDetailTabIndex => 0;
        public SpyTransactionsTabViewModel TransactionsSpy { get; } = new();
        public TransactionsTabViewModel Transactions => TransactionsSpy;
        public CreditsTabViewModel Credits { get; } = new(
            null, () => false, () => string.Empty, () => string.Empty, () => string.Empty,
            _ => { }, (_, _, _) => { });
        public PriceHistoryTabViewModel PriceHistory { get; } = new(
            null, () => false, () => string.Empty, () => string.Empty, () => string.Empty,
            _ => { }, (_, _, _) => { });
        public AssetDetailsDTO? LastAssetDetails { get; private set; }
        public decimal? LastRealizedPortfolioWeight { get; private set; }

        public void LoadAssetDetails(AssetDetailsDTO details, decimal? realizedPortfolioWeight = null)
        {
            LastAssetDetails = details;
            LastRealizedPortfolioWeight = realizedPortfolioWeight;
        }

        public void LoadBrokerSummary(string brokerName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits)
        {
            LastBrokerSummary = summary;
            WasBrokerSummaryLoaded = true;
        }

        public Task LoadBrokerBreakdown(string brokerName)
        {
            LastBrokerBreakdownName = brokerName;
            WasBrokerBreakdownLoaded = true;
            return Task.CompletedTask;
        }

        public void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits) { }

        public void LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems)
        {
            LastPortfolioSummary = summary;
            LastPortfolioAssetItems = assetItems;
            WasPortfolioSummaryLoaded = true;
        }

        public void Clear() => WasCleared = true;
        public Task EnsureTodayInfoLoadedAsync() => Task.CompletedTask;
    }

    internal sealed class SpyTransactionsTabViewModel : TransactionsTabViewModel
    {
        public string? LastBrokerTransactionsName { get; private set; }
        public bool WasBrokerTransactionsLoaded { get; private set; }
        public string? LastPortfolioTransactionsBrokerName { get; private set; }
        public string? LastPortfolioTransactionsPortfolioName { get; private set; }
        public bool WasPortfolioTransactionsLoaded { get; private set; }

        public SpyTransactionsTabViewModel() : base(
            null, new StubTransactionQueryService(), InvestmentScope.Active,
            () => false, () => string.Empty, () => string.Empty, () => string.Empty,
            _ => { }, (_, _, _) => { })
        {
        }

        public override Task LoadBroker(string brokerName)
        {
            LastBrokerTransactionsName = brokerName;
            WasBrokerTransactionsLoaded = true;
            return Task.CompletedTask;
        }

        public override Task LoadPortfolio(string brokerName, string portfolioName)
        {
            LastPortfolioTransactionsBrokerName = brokerName;
            LastPortfolioTransactionsPortfolioName = portfolioName;
            WasPortfolioTransactionsLoaded = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPortfolioAssetSummaryService : IPortfolioAssetSummaryService
    {
        public IReadOnlyList<PortfolioAssetSummaryItemDTO> Items { get; set; } = [];
        public string? LastBrokerName { get; private set; }
        public string? LastPortfolioName { get; private set; }
        public InvestmentScope? LastScope { get; private set; }

        public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
        {
            LastBrokerName = brokerName;
            LastPortfolioName = portfolioName;
            LastScope = scope;
            return Items;
        }
    }

    internal sealed class StubSummaryService : ISummaryService
    {
        public AggregatedSummaryDTO BrokerSummary { get; set; } = new();
        public AggregatedSummaryDTO PortfolioSummary { get; set; } = new();
        public string? LastBrokerNameForBroker { get; private set; }
        public string? LastBrokerNameForPortfolio { get; private set; }
        public string? LastPortfolioName { get; private set; }
        public InvestmentScope? LastScopeForBroker { get; private set; }
        public InvestmentScope? LastScopeForPortfolio { get; private set; }

        public AggregatedSummaryDTO GetBrokerSummary(string brokerName, InvestmentScope scope = InvestmentScope.Active)
        {
            LastBrokerNameForBroker = brokerName;
            LastScopeForBroker = scope;
            return BrokerSummary;
        }

        public AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
        {
            LastBrokerNameForPortfolio = brokerName;
            LastPortfolioName = portfolioName;
            LastScopeForPortfolio = scope;
            return PortfolioSummary;
        }
    }

    internal sealed class StubNavigationService : INavigationService
    {
        public InvestmentScope? LastTreeScope { get; private set; }
        public InvestmentScope? LastAssetDetailsScope { get; private set; }
        public AssetDetailsDTO? AssetDetails { get; set; }

        /// <summary>Overrides the empty default for tests that need a tree to select within.</summary>
        public TreeNodeDTO? Tree { get; set; }

        public TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active)
        {
            LastTreeScope = scope;
            return Tree ?? new() { NodeType = TreeNodeType.Broker, DisplayName = "Root", Metadata = [], Children = [] };
        }

        public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active)
        {
            LastAssetDetailsScope = scope;
            return AssetDetails;
        }

        public IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active) => [];
        public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName) => [];
    }

    private sealed class StubCreditQueryService : ICreditQueryService
    {
        public InvestmentScope? LastBrokerScope { get; private set; }
        public InvestmentScope? LastPortfolioScope { get; private set; }

        public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active)
        {
            LastBrokerScope = scope;
            return [];
        }

        public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
        {
            LastPortfolioScope = scope;
            return [];
        }
    }
}
