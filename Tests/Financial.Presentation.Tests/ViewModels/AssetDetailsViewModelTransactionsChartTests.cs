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
        var vm = BuildViewModel();
        _ = vm.LoadBrokerTransactions("XPI");
        vm.IsTransactionsLoading.Should().BeTrue();
    }

    [Fact]
    public async Task LoadBrokerTransactions_PopulatesTransactionsPlotModel_OnSuccess()
    {
        _transactionQueryService.BrokerTransactions = [new() { AssetName = "BBAS3", Date = DateTime.Today, Type = "Buy", TotalPrice = 1000m }];
        var vm = BuildViewModel();

        await vm.LoadBrokerTransactions("XPI");

        vm.TransactionsPlotModel.Should().NotBeNull();
        vm.TransactionsPlotModel!.Series.Should().HaveCount(1);
        vm.IsTransactionsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadBrokerTransactions_SetsTransactionsError_OnFailure()
    {
        _transactionQueryService.ExceptionToThrow = new InvalidOperationException("boom");
        var vm = BuildViewModel();

        await vm.LoadBrokerTransactions("XPI");

        vm.TransactionsError.Should().NotBeNull();
        vm.IsTransactionsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPortfolioTransactions_PassesCorrectBrokerAndPortfolioName()
    {
        var vm = BuildViewModel();

        await vm.LoadPortfolioTransactions("XPI", "Acoes");

        _transactionQueryService.LastPortfolioBrokerName.Should().Be("XPI");
        _transactionQueryService.LastPortfolioName.Should().Be("Acoes");
    }

    [Fact]
    public async Task LoadBrokerTransactions_RequestsActiveScope()
    {
        var vm = BuildViewModel();

        await vm.LoadBrokerTransactions("XPI");

        _transactionQueryService.LastBrokerScope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public async Task LoadBrokerTransactions_HistoricScope_RequestsHistoricScope()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        await vm.LoadBrokerTransactions("XPI");

        _transactionQueryService.LastBrokerScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public async Task LoadPortfolioTransactions_HistoricScope_RequestsHistoricScope()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        await vm.LoadPortfolioTransactions("XPI", "Acoes");

        _transactionQueryService.LastPortfolioScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void LoadAssetDetails_BuildsTransactionsPlotModel_FromAlreadyLoadedTransactions_NoNewFetch()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails([
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 100m, UnitPrice = 20m, Fees = 0m, TotalPrice = 2000m },
        ]));

        vm.TransactionsPlotModel.Should().NotBeNull();
        vm.TransactionsPlotModel!.Series.Should().HaveCount(1);
        _transactionQueryService.LastBrokerName.Should().BeNull();
        _transactionQueryService.LastPortfolioBrokerName.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_SetsIsTransactionsAggregateViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails([]));
        vm.IsTransactionsAggregateView.Should().BeFalse();
    }

    [Fact]
    public void LoadBrokerSummary_SetsIsTransactionsAggregateViewTrue()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.IsTransactionsAggregateView.Should().BeTrue();
    }

    [Fact]
    public void SetTransactionsFilter_PersistsSelectionPerNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.SelectTransactionsFilterCommand.Execute(PeriodFilter.Ytd);
        vm.TransactionsFilters.First(f => f.Filter == PeriodFilter.Ytd).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerB", new AggregatedSummaryDTO(), []);
        vm.TransactionsFilters.First(f => f.Filter == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);
        vm.TransactionsFilters.First(f => f.Filter == PeriodFilter.Ytd).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetTransactionsChartMode_PersistsSelectionPerNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.SelectTransactionsChartModeCommand.Execute(ChartTypeMode.Line);
        vm.ChartTypeModes.First(m => m.Mode == ChartTypeMode.Line).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerB", new AggregatedSummaryDTO(), []);
        vm.ChartTypeModes.First(m => m.Mode == ChartTypeMode.Bar).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);
        vm.ChartTypeModes.First(m => m.Mode == ChartTypeMode.Line).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetTransactionsFilter_DoesNotAffectCreditsFilter_ForSameNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.SelectTransactionsFilterCommand.Execute(PeriodFilter.Ytd);

        vm.CreditsFilters.First(f => f.Filter == PeriodFilter.Last12Months).IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task Clear_AfterLoadBrokerTransactions_ResetsTransactionsState()
    {
        _transactionQueryService.BrokerTransactions = [new() { AssetName = "BBAS3", Date = DateTime.Today, Type = "Buy", TotalPrice = 500m }];
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        await vm.LoadBrokerTransactions("XPI");

        vm.Clear();

        vm.TransactionsPlotModel.Should().BeNull();
        vm.IsTransactionsLoading.Should().BeFalse();
        vm.TransactionsError.Should().BeNull();
        vm.IsTransactionsAggregateView.Should().BeFalse();
    }

}
