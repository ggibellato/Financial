using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelXirrTests
{
    private static AssetDetailsViewModel BuildViewModel(IAssetPriceService? priceService = null, InvestmentScope scope = InvestmentScope.Active)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            priceService ?? new FixedPriceService(0m),
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new XirrCalculationService(),
            scope);
    }

    private static AssetDetailsDTO BuildAssetDetails(
        IReadOnlyList<AssetCashFlowDTO> cashFlowsWithoutCredits,
        IReadOnlyList<AssetCashFlowDTO> cashFlowsWithCredits,
        decimal totalCredits = 0m,
        decimal realizedGainLoss = 0m) => new()
    {
        Name = "TEST",
        BrokerName = "XPI",
        PortfolioName = "Default",
        Ticker = "TEST",
        Exchange = "BVMF",
        Quantity = 1m,
        AveragePrice = 1000m,
        TotalCredits = totalCredits,
        RealizedGainLoss = realizedGainLoss,
        CashFlowsWithoutCredits = cashFlowsWithoutCredits,
        CashFlowsWithCredits = cashFlowsWithCredits
    };

    [Fact]
    public void Xirr_BeforeAssetLoaded_IsNull()
    {
        var vm = BuildViewModel();

        vm.Xirr.Should().BeNull();
        vm.XirrWithCredits.Should().BeNull();
    }

    [Fact]
    public async Task Xirr_AfterLoadAssetDetailsAndPriceFetched_ComputesApproximateRate()
    {
        var buyDate = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = buyDate, Amount = -1000m } };
        var vm = BuildViewModel(new FixedPriceService(1100m));

        vm.LoadAssetDetails(BuildAssetDetails(cashFlows, cashFlows));
        await vm.EnsureTodayInfoLoadedAsync();

        vm.Xirr.Should().NotBeNull();
        vm.Xirr!.Value.Should().BeApproximately(0.10m, 0.01m);
    }

    [Fact]
    public async Task EnsureTodayInfoLoadedAsync_HistoricScope_DoesNotFetchPriceOrChangeState()
    {
        var buyDate = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = buyDate, Amount = -1000m } };
        var priceService = new FixedPriceService(1100m);
        var vm = BuildViewModel(priceService, scope: InvestmentScope.Historic);

        vm.LoadAssetDetails(BuildAssetDetails(cashFlows, cashFlows));
        await vm.EnsureTodayInfoLoadedAsync();

        priceService.CallCount.Should().Be(0);
        vm.TodayCurrentValue.Should().Be(0m);
        vm.TodayCurrentValueAsOf.Should().BeEmpty();
        vm.Xirr.Should().BeNull();
    }

    [Fact]
    public async Task XirrWithCredits_UsesCreditsCashFlowsAndTotalWithCredits()
    {
        var buyDate = DateTime.Today.AddYears(-1);
        var creditDate = DateTime.Today.AddMonths(-6);
        var withoutCredits = new List<AssetCashFlowDTO> { new() { Date = buyDate, Amount = -1000m } };
        var withCredits = new List<AssetCashFlowDTO>
        {
            new() { Date = buyDate, Amount = -1000m },
            new() { Date = creditDate, Amount = 50m }
        };
        var vm = BuildViewModel(new FixedPriceService(1000m));

        vm.LoadAssetDetails(BuildAssetDetails(withoutCredits, withCredits, totalCredits: 50m));
        await vm.EnsureTodayInfoLoadedAsync();

        vm.Xirr.Should().NotBeNull();
        vm.XirrWithCredits.Should().NotBeNull();
        vm.XirrWithCredits!.Value.Should().BeGreaterThan(vm.Xirr!.Value);
    }

    [Fact]
    public void Clear_ResetsXirrToNull()
    {
        var buyDate = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = buyDate, Amount = -1000m } };
        var vm = BuildViewModel(new FixedPriceService(1100m));
        vm.LoadAssetDetails(BuildAssetDetails(cashFlows, cashFlows));

        vm.Clear();

        vm.Xirr.Should().BeNull();
        vm.XirrWithCredits.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_SetsRealizedGainLossFromDto()
    {
        var buyDate = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = buyDate, Amount = -1000m } };
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails(cashFlows, cashFlows, realizedGainLoss: 62m));

        vm.RealizedGainLoss.Should().Be(62m);
    }

    [Fact]
    public void Clear_ResetsRealizedGainLossToZero()
    {
        var buyDate = DateTime.Today.AddYears(-1);
        var cashFlows = new List<AssetCashFlowDTO> { new() { Date = buyDate, Amount = -1000m } };
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails(cashFlows, cashFlows, realizedGainLoss: 62m));

        vm.Clear();

        vm.RealizedGainLoss.Should().Be(0m);
    }

    private sealed class FixedPriceService : IAssetPriceService
    {
        private readonly decimal _price;

        public int CallCount { get; private set; }

        public FixedPriceService(decimal price)
        {
            _price = price;
        }

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            CallCount++;
            return new() { Exchange = request.Exchange, Ticker = request.Ticker, Price = _price, AsOf = DateTimeOffset.UtcNow };
        }
    }
}
