using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelXirrTests
{
    private static AssetDetailsViewModel BuildViewModel(IAssetPriceService? priceService = null)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            priceService ?? new FixedPriceService(0m),
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new XirrCalculationService());
    }

    private static AssetDetailsDTO BuildAssetDetails(
        IReadOnlyList<AssetCashFlowDTO> cashFlowsWithoutCredits,
        IReadOnlyList<AssetCashFlowDTO> cashFlowsWithCredits,
        decimal totalCredits = 0m) => new()
    {
        Name = "TEST",
        BrokerName = "XPI",
        PortfolioName = "Default",
        Ticker = "TEST",
        Exchange = "BVMF",
        Quantity = 1m,
        AveragePrice = 1000m,
        TotalCredits = totalCredits,
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

    private sealed class FixedPriceService : IAssetPriceService
    {
        private readonly decimal _price;

        public FixedPriceService(decimal price)
        {
            _price = price;
        }

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) =>
            new() { Exchange = request.Exchange, Ticker = request.Ticker, Price = _price, AsOf = DateTimeOffset.UtcNow };
    }
}
