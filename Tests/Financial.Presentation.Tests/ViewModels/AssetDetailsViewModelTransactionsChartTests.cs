using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelTransactionsChartTests
{
    private readonly StubTransactionQueryService _transactionQueryService;

    public AssetDetailsViewModelTransactionsChartTests()
    {
        _transactionQueryService = new StubTransactionQueryService();
    }

    /// <summary>Builds the view model over the shared transaction-query stub; scope and the breakdown
    /// service are the only things these tests vary.</summary>
    private AssetDetailsViewModel BuildViewModel(
        IBrokerBreakdownService? brokerBreakdownService = null,
        ITransactionQueryService? transactionQueryService = null,
        InvestmentScope scope = InvestmentScope.Active)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            brokerBreakdownService ?? new StubBrokerBreakdownService(),
            transactionQueryService ?? _transactionQueryService,
            new XirrCalculationService(),
            new ProfitCalculationService(),
            scope);
    }

    private static AssetDetailsDTO BuildAssetDetails(List<TransactionDTO> transactions) => new()
    {
        Name = "BBAS3",
        BrokerName = "XPI",
        PortfolioName = "Acoes",
        Ticker = "BBAS3",
        ISIN = "",
        Exchange = "BVMF",
        Country = Financial.Investment.Domain.Entities.CountryCode.Unknown,
        LocalTypeCode = "",
        Class = Financial.Investment.Domain.Entities.GlobalAssetClass.Unknown,
        Quantity = 100m,
        AveragePrice = 20m,
        TotalBought = 2000m,
        TotalSold = 0m,
        TotalCredits = 0m,
        Transactions = transactions,
        Credits = [],
    };

    [Fact]
    public void LoadBrokerTransactions_SetsIsTransactionsLoadingTrue_Synchronously()
    {
        // Uses a blocking stub rather than the shared StubTransactionQueryService: the latter
        // returns instantly, so the background Task.Run could reach ApplyFetchedTransactions
        // (which sets IsTransactionsLoading = false) before this synchronous assertion runs -
        // a real, previously-observed race (flaked in CI 2026-08-21), not a hypothetical one.
        var vm = BuildViewModel(transactionQueryService: new BlockingTransactionQueryService());
        _ = vm.Transactions.LoadBroker("XPI");
        vm.Transactions.IsTransactionsLoading.Should().BeTrue();
    }

    [Fact]
    public async Task LoadBrokerTransactions_PopulatesTransactionsPlotModel_OnSuccess()
    {
        _transactionQueryService.BrokerTransactions = [new() { AssetName = "BBAS3", Date = DateTime.Today, Type = "Buy", TotalPrice = 1000m }];
        var vm = BuildViewModel();

        await vm.Transactions.LoadBroker("XPI");

        vm.Transactions.TransactionsPlotModel.Should().NotBeNull();
        vm.Transactions.TransactionsPlotModel!.Series.Should().HaveCount(1);
        vm.Transactions.IsTransactionsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadBrokerTransactions_SetsTransactionsError_OnFailure()
    {
        _transactionQueryService.ExceptionToThrow = new InvalidOperationException("boom");
        var vm = BuildViewModel();

        await vm.Transactions.LoadBroker("XPI");

        vm.Transactions.TransactionsError.Should().NotBeNull();
        vm.Transactions.IsTransactionsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPortfolioTransactions_PassesCorrectBrokerAndPortfolioName()
    {
        var vm = BuildViewModel();

        await vm.Transactions.LoadPortfolio("XPI", "Acoes");

        _transactionQueryService.LastPortfolioBrokerName.Should().Be("XPI");
        _transactionQueryService.LastPortfolioName.Should().Be("Acoes");
    }

    [Fact]
    public async Task LoadBrokerTransactions_RequestsActiveScope()
    {
        var vm = BuildViewModel();

        await vm.Transactions.LoadBroker("XPI");

        _transactionQueryService.LastBrokerScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public async Task LoadBrokerTransactions_HistoricScope_RequestsHistoricScope()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        await vm.Transactions.LoadBroker("XPI");

        _transactionQueryService.LastBrokerScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public async Task LoadPortfolioTransactions_HistoricScope_RequestsHistoricScope()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        await vm.Transactions.LoadPortfolio("XPI", "Acoes");

        _transactionQueryService.LastPortfolioScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void LoadAssetDetails_BuildsTransactionsPlotModel_FromAlreadyLoadedTransactions_NoNewFetch()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails([
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 100m, UnitPrice = 20m, Fees = 0m, TotalPrice = 2000m },
        ]));

        vm.Transactions.TransactionsPlotModel.Should().NotBeNull();
        vm.Transactions.TransactionsPlotModel!.Series.Should().HaveCount(1);
        _transactionQueryService.LastBrokerName.Should().BeNull();
        _transactionQueryService.LastPortfolioBrokerName.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_SetsIsTransactionsAggregateViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails([]));
        vm.Transactions.IsTransactionsAggregateView.Should().BeFalse();
    }

    [Fact]
    public void LoadBrokerSummary_SetsIsTransactionsAggregateViewTrue()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.Transactions.IsTransactionsAggregateView.Should().BeTrue();
    }

    [Fact]
    public void SetTransactionsFilter_PersistsSelectionPerNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.Transactions.SelectTransactionsFilterCommand.Execute(PeriodFilter.Ytd);
        vm.Transactions.TransactionsFilters.First(f => f.Filter == PeriodFilter.Ytd).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerB", new AggregatedSummaryDTO(), []);
        vm.Transactions.TransactionsFilters.First(f => f.Filter == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);
        vm.Transactions.TransactionsFilters.First(f => f.Filter == PeriodFilter.Ytd).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetTransactionsChartMode_PersistsSelectionPerNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.Transactions.SelectTransactionsChartModeCommand.Execute(ChartTypeMode.Line);
        vm.Transactions.ChartTypeModes.First(m => m.Mode == ChartTypeMode.Line).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerB", new AggregatedSummaryDTO(), []);
        vm.Transactions.ChartTypeModes.First(m => m.Mode == ChartTypeMode.Bar).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);
        vm.Transactions.ChartTypeModes.First(m => m.Mode == ChartTypeMode.Line).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetTransactionsFilter_DoesNotAffectCreditsFilter_ForSameNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.Transactions.SelectTransactionsFilterCommand.Execute(PeriodFilter.Ytd);

        vm.Credits.CreditsFilters.First(f => f.Filter == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task Clear_AfterLoadBrokerTransactions_ResetsTransactionsState()
    {
        _transactionQueryService.BrokerTransactions = [new() { AssetName = "BBAS3", Date = DateTime.Today, Type = "Buy", TotalPrice = 500m }];
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        await vm.Transactions.LoadBroker("XPI");

        vm.Clear();

        vm.Transactions.TransactionsPlotModel.Should().BeNull();
        vm.Transactions.IsTransactionsLoading.Should().BeFalse();
        vm.Transactions.TransactionsError.Should().BeNull();
        vm.Transactions.IsTransactionsAggregateView.Should().BeFalse();
    }

    private sealed class BlockingTransactionQueryService : ITransactionQueryService
    {
        // Bounded, not infinite: same reasoning as NeverResolvingPriceService
        // (AssetDetailsViewModelPortfolioSummaryTests.cs) - the test only needs the block to
        // outlast its own synchronous assertion, and an unbounded wait would accumulate
        // permanently-blocked threads across the test run.
        private readonly SemaphoreSlim _blocker = new(0);
        private static readonly TimeSpan MaxBlockDuration = TimeSpan.FromSeconds(2);

        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active)
        {
            _blocker.Wait(MaxBlockDuration);
            return [];
        }

        public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
        {
            _blocker.Wait(MaxBlockDuration);
            return [];
        }
    }
}
