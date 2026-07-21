using Financial.Application.DTOs;
using Financial.Application.Enums;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class MainNavigationViewModelBaseTests
{
    [Fact]
    public void AssetClassFilters_IncludesCryptocurrencyWithCorrectLabel()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        vm.AssetClassFilters.Should().ContainSingle(f =>
            f.Filter == GlobalAssetClass.Cryptocurrency && f.Label == "Cryptocurrency");
    }

    [Fact]
    public void SelectingPortfolioNode_LoadsTotalSoldFromSummaryService()
    {
        var summary = new AggregatedSummaryDTO { TotalBought = 10000m, TotalSold = 4706.65m, TotalCredits = 500m };
        var summaryService = new StubSummaryService { PortfolioSummary = summary };
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        spy.LastPortfolioSummary.Should().NotBeNull();
        spy.LastPortfolioSummary!.TotalSold.Should().Be(4706.65m);
        spy.LastPortfolioSummary.TotalBought.Should().Be(10000m);
    }

    [Fact]
    public void SelectingBrokerNode_LoadsTotalSoldFromSummaryService()
    {
        var summary = new AggregatedSummaryDTO { TotalBought = 20000m, TotalSold = 9000m, TotalCredits = 800m };
        var summaryService = new StubSummaryService { BrokerSummary = summary };
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        spy.LastBrokerSummary.Should().NotBeNull();
        spy.LastBrokerSummary!.TotalSold.Should().Be(9000m);
        spy.LastBrokerSummary.TotalBought.Should().Be(20000m);
    }

    [Fact]
    public void SelectingPortfolioNode_PassesCorrectBrokerAndPortfolioNameToSummaryService()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        summaryService.LastBrokerNameForPortfolio.Should().Be("XPI");
        summaryService.LastPortfolioName.Should().Be("FII");
    }

    [Fact]
    public void SelectingBrokerNode_PassesCorrectBrokerNameToSummaryService()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        summaryService.LastBrokerNameForBroker.Should().Be("XPI");
    }

    [Fact]
    public void SelectingBrokerNode_CallsLoadBrokerSummaryOnDetailsViewModel()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        spy.WasBrokerSummaryLoaded.Should().BeTrue();
        spy.LastBrokerSummary.Should().NotBeNull();
    }

    [Fact]
    public void SelectingBrokerNode_CallsLoadBrokerBreakdownOnDetailsViewModel()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        spy.WasBrokerBreakdownLoaded.Should().BeTrue();
        spy.LastBrokerBreakdownName.Should().Be("XPI");
    }

    [Fact]
    public void SelectingBrokerNode_CallsLoadBrokerTransactionsOnDetailsViewModel()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        spy.WasBrokerTransactionsLoaded.Should().BeTrue();
        spy.LastBrokerTransactionsName.Should().Be("XPI");
    }

    [Fact]
    public void SelectingPortfolioNode_CallsLoadPortfolioTransactionsOnDetailsViewModel()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        spy.WasPortfolioTransactionsLoaded.Should().BeTrue();
        spy.LastPortfolioTransactionsBrokerName.Should().Be("XPI");
        spy.LastPortfolioTransactionsPortfolioName.Should().Be("FII");
    }

    [Fact]
    public void SelectingPortfolioNode_WhenMissingMetadata_ClearsDetails()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var nodeWithoutMetadata = BuildNodeWithoutMetadata(TreeNodeType.Portfolio);
        vm.SelectedNode = nodeWithoutMetadata;

        spy.WasCleared.Should().BeTrue();
        spy.LastPortfolioSummary.Should().BeNull();
    }

    [Fact]
    public void SelectingPortfolioNode_CallsLoadPortfolioSummaryOnDetailsViewModel()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        spy.WasPortfolioSummaryLoaded.Should().BeTrue();
        spy.LastPortfolioSummary.Should().NotBeNull();
    }

    [Fact]
    public void SelectingPortfolioNode_PassesCorrectAssetItemsFromService()
    {
        var summaryService = new StubSummaryService();
        var assetSummaryService = new StubPortfolioAssetSummaryService
        {
            Items =
            [
                new PortfolioAssetSummaryItemDTO { AssetName = "A", Ticker = "A", Exchange = "LSE", TotalInvested = 100m },
                new PortfolioAssetSummaryItemDTO { AssetName = "B", Ticker = "B", Exchange = "LSE", TotalInvested = 200m }
            ]
        };
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy, assetSummaryService);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        spy.LastPortfolioAssetItems.Should().HaveCount(2);
    }

    [Fact]
    public void SelectingPortfolioNode_PassesCorrectBrokerAndPortfolioNameToAssetSummaryService()
    {
        var summaryService = new StubSummaryService();
        var assetSummaryService = new StubPortfolioAssetSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy, assetSummaryService);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        assetSummaryService.LastBrokerName.Should().Be("XPI");
        assetSummaryService.LastPortfolioName.Should().Be("FII");
    }

    [Fact]
    public void SelectingAssetNode_DoesNotCallLoadBrokerOrPortfolioTransactions()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var assetNode = BuildAssetNode("XPI", "Acoes", "BBAS3");
        vm.SelectedNode = assetNode;

        spy.WasBrokerTransactionsLoaded.Should().BeFalse();
        spy.WasPortfolioTransactionsLoaded.Should().BeFalse();
    }

    [Fact]
    public void SelectingBrokerNode_DoesNotCallLoadPortfolioSummary()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        spy.WasPortfolioSummaryLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task LoadNavigationTreeAsync_RequestsActiveScope()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var navigationService = new StubNavigationService();
        var vm = new TestableNavigationViewModel(summaryService, spy, navigationService: navigationService);

        // GetNavigationTree runs (and records its scope) before ApplyAssetClassFilter's
        // System.Windows.Application.Current.Dispatcher.Invoke, which throws here because no
        // WPF Application runs in this test host; only the scope pass-through is under test.
        try
        {
            await vm.LoadNavigationTreeAsync();
        }
        catch (NullReferenceException)
        {
        }

        navigationService.LastTreeScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingAssetNode_RequestsActiveScopeAssetDetails()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var navigationService = new StubNavigationService();
        var vm = new TestableNavigationViewModel(summaryService, spy, navigationService: navigationService);

        var assetNode = BuildAssetNode("XPI", "Acoes", "BBAS3");
        vm.SelectedNode = assetNode;

        navigationService.LastAssetDetailsScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingPortfolioNode_RequestsActiveScopeSummaryAndAssetItems()
    {
        var summaryService = new StubSummaryService();
        var assetSummaryService = new StubPortfolioAssetSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy, assetSummaryService);

        var portfolioNode = BuildPortfolioNode("XPI", "FII");
        vm.SelectedNode = portfolioNode;

        summaryService.LastScopeForPortfolio.Should().Be(InvestmentScope.Active);
        assetSummaryService.LastScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void SelectingBrokerNode_RequestsActiveScopeSummary()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        summaryService.LastScopeForBroker.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public async Task LoadNavigationTreeAsync_HistoricScope_RequestsHistoricScope()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var navigationService = new StubNavigationService();
        var vm = new TestableNavigationViewModel(summaryService, spy, navigationService: navigationService, scope: InvestmentScope.Historic);

        try
        {
            await vm.LoadNavigationTreeAsync();
        }
        catch (NullReferenceException)
        {
        }

        navigationService.LastTreeScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingAssetNode_HistoricScope_RequestsHistoricScopeAssetDetails()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var navigationService = new StubNavigationService();
        var vm = new TestableNavigationViewModel(summaryService, spy, navigationService: navigationService, scope: InvestmentScope.Historic);

        var assetNode = BuildAssetNode("XPI", "Uncategorized", "BBAS3");
        vm.SelectedNode = assetNode;

        navigationService.LastAssetDetailsScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingAssetNode_HistoricScope_PassesMatchingPortfolioWeightToAssetDetails()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var navigationService = new StubNavigationService
        {
            AssetDetails = new AssetDetailsDTO { Name = "BBAS3", BrokerName = "XPI", PortfolioName = "Uncategorized", Ticker = "BBAS3" }
        };
        var assetSummaryService = new StubPortfolioAssetSummaryService
        {
            Items =
            [
                new PortfolioAssetSummaryItemDTO { AssetName = "Other Asset", PortfolioWeight = 40m },
                new PortfolioAssetSummaryItemDTO { AssetName = "BBAS3", PortfolioWeight = 5.15m }
            ]
        };
        var vm = new TestableNavigationViewModel(summaryService, spy, assetSummaryService, navigationService, InvestmentScope.Historic);

        var assetNode = BuildAssetNode("XPI", "Uncategorized", "BBAS3");
        vm.SelectedNode = assetNode;

        spy.LastAssetDetails.Should().NotBeNull();
        spy.LastRealizedPortfolioWeight.Should().Be(5.15m);
        assetSummaryService.LastBrokerName.Should().Be("XPI");
        assetSummaryService.LastPortfolioName.Should().Be("Uncategorized");
        assetSummaryService.LastScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingAssetNode_ActiveScope_DoesNotFetchPortfolioWeight()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var navigationService = new StubNavigationService
        {
            AssetDetails = new AssetDetailsDTO { Name = "BBAS3", BrokerName = "XPI", PortfolioName = "Acoes", Ticker = "BBAS3" }
        };
        var assetSummaryService = new StubPortfolioAssetSummaryService();
        var vm = new TestableNavigationViewModel(summaryService, spy, assetSummaryService, navigationService);

        var assetNode = BuildAssetNode("XPI", "Acoes", "BBAS3");
        vm.SelectedNode = assetNode;

        assetSummaryService.LastBrokerName.Should().BeNull();
        spy.LastRealizedPortfolioWeight.Should().BeNull();
    }

    [Fact]
    public void SelectingPortfolioNode_HistoricScope_RequestsHistoricScopeSummaryAndAssetItems()
    {
        var summaryService = new StubSummaryService();
        var assetSummaryService = new StubPortfolioAssetSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy, assetSummaryService, scope: InvestmentScope.Historic);

        var portfolioNode = BuildPortfolioNode("XPI", "Uncategorized");
        vm.SelectedNode = portfolioNode;

        summaryService.LastScopeForPortfolio.Should().Be(InvestmentScope.Historic);
        assetSummaryService.LastScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void SelectingBrokerNode_HistoricScope_RequestsHistoricScopeSummary()
    {
        var summaryService = new StubSummaryService();
        var spy = new SpyAssetDetailsViewModel();
        var vm = new TestableNavigationViewModel(summaryService, spy, scope: InvestmentScope.Historic);

        var brokerNode = BuildBrokerNode("XPI");
        vm.SelectedNode = brokerNode;

        summaryService.LastScopeForBroker.Should().Be(InvestmentScope.Historic);
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

    private sealed class TestableNavigationViewModel : MainNavigationViewModelBase<SpyAssetDetailsViewModel>
    {
        public StubNavigationService NavigationService { get; }

        public TestableNavigationViewModel(
            ISummaryService summaryService,
            SpyAssetDetailsViewModel spy,
            IPortfolioAssetSummaryService? portfolioAssetSummaryService = null,
            StubNavigationService? navigationService = null,
            InvestmentScope scope = InvestmentScope.Active)
            : this(navigationService ?? new StubNavigationService(), summaryService, spy, portfolioAssetSummaryService, scope)
        {
        }

        private TestableNavigationViewModel(
            StubNavigationService navigationService,
            ISummaryService summaryService,
            SpyAssetDetailsViewModel spy,
            IPortfolioAssetSummaryService? portfolioAssetSummaryService,
            InvestmentScope scope)
            : base(navigationService, new StubCreditQueryService(), summaryService, portfolioAssetSummaryService ?? new StubPortfolioAssetSummaryService(), spy, scope)
        {
            NavigationService = navigationService;
        }
    }

    private sealed class SpyAssetDetailsViewModel : IAssetDetailsViewModel
    {
        public AggregatedSummaryDTO? LastPortfolioSummary { get; private set; }
        public AggregatedSummaryDTO? LastBrokerSummary { get; private set; }
        public IReadOnlyList<PortfolioAssetSummaryItemDTO>? LastPortfolioAssetItems { get; private set; }
        public bool WasCleared { get; private set; }
        public bool WasPortfolioSummaryLoaded { get; private set; }
        public bool WasBrokerSummaryLoaded { get; private set; }
        public string? LastBrokerBreakdownName { get; private set; }
        public bool WasBrokerBreakdownLoaded { get; private set; }
        public string? LastBrokerTransactionsName { get; private set; }
        public bool WasBrokerTransactionsLoaded { get; private set; }
        public string? LastPortfolioTransactionsBrokerName { get; private set; }
        public string? LastPortfolioTransactionsPortfolioName { get; private set; }
        public bool WasPortfolioTransactionsLoaded { get; private set; }
        public bool IsPortfolioView => false;
        public bool IsBrokerView => false;
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

        public Task LoadBrokerTransactions(string brokerName)
        {
            LastBrokerTransactionsName = brokerName;
            WasBrokerTransactionsLoaded = true;
            return Task.CompletedTask;
        }

        public void LoadPortfolioCredits(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits) { }

        public void LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems)
        {
            LastPortfolioSummary = summary;
            LastPortfolioAssetItems = assetItems;
            WasPortfolioSummaryLoaded = true;
        }

        public Task LoadPortfolioTransactions(string brokerName, string portfolioName)
        {
            LastPortfolioTransactionsBrokerName = brokerName;
            LastPortfolioTransactionsPortfolioName = portfolioName;
            WasPortfolioTransactionsLoaded = true;
            return Task.CompletedTask;
        }

        public void Clear() => WasCleared = true;
        public Task EnsureTodayInfoLoadedAsync() => Task.CompletedTask;
        public void UpdateCreditsPlotWidth(double plotWidth) { }
        public void UpdateTransactionsPlotWidth(double plotWidth) { }
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

    private sealed class StubSummaryService : ISummaryService
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

    private sealed class StubNavigationService : INavigationService
    {
        public InvestmentScope? LastTreeScope { get; private set; }
        public InvestmentScope? LastAssetDetailsScope { get; private set; }
        public AssetDetailsDTO? AssetDetails { get; set; }

        public TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active)
        {
            LastTreeScope = scope;
            return new() { NodeType = TreeNodeType.Broker, DisplayName = "Root", Metadata = [], Children = [] };
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
        public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName) => [];
        public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName) => [];
    }
}
