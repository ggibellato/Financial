using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelXirrTests
{
    private static AssetDetailsViewModel BuildViewModel(
        IAssetPriceLookupService? priceService = null,
        InvestmentScope scope = InvestmentScope.Active,
        INavigationService? navigationService = null)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new NotUsedAssetPriceService(),
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            navigationService ?? new FakeNavigationService(),
            new FakePortfolioAssetSummaryService(),
            new ProfitCalculationService(),
            scope,
            priceService ?? new FixedPriceService(0m));
    }

    private static AssetDetailsDTO BuildAssetDetails(
        decimal totalCredits = 0m,
        decimal realizedGainLoss = 0m,
        decimal? marketValue = null,
        decimal costOfUnitsHeld = 1000m,
        decimal? unrealisedGain = null,
        decimal? priceOnlyReturn = null,
        decimal? totalReturn = null) => new()
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
        MarketValue = marketValue,
        CostOfUnitsHeld = costOfUnitsHeld,
        UnrealisedGain = unrealisedGain,
        PriceOnlyReturn = priceOnlyReturn,
        TotalReturn = totalReturn,
    };

    [Fact]
    public void Xirr_BeforeAssetLoaded_IsNull()
    {
        var vm = BuildViewModel();

        vm.Xirr.Should().BeNull();
        vm.XirrWithCredits.Should().BeNull();
    }

    [Fact]
    public async Task Xirr_AfterPriceFetchSettles_ReflectsRefreshedAssetDetails()
    {
        var navigationService = new FakeNavigationService { AssetDetails = BuildAssetDetails(priceOnlyReturn: 0.10m) };
        var vm = BuildViewModel(new FixedPriceService(1100m), navigationService: navigationService);

        vm.LoadAssetDetails(BuildAssetDetails());
        await vm.EnsureTodayInfoLoadedAsync();

        vm.Xirr.Should().Be(0.10m);
    }

    [Fact]
    public async Task EnsureTodayInfoLoadedAsync_HistoricScope_DoesNotFetchPriceOrChangeState()
    {
        var priceService = new FixedPriceService(1100m);
        var vm = BuildViewModel(priceService, scope: InvestmentScope.Historic);

        vm.LoadAssetDetails(BuildAssetDetails());
        await vm.EnsureTodayInfoLoadedAsync();

        priceService.CallCount.Should().Be(0);
        vm.TodayCurrentValue.Should().Be(0m);
        vm.TodayCurrentValueAsOf.Should().BeEmpty();
        vm.Xirr.Should().BeNull();
    }

    [Fact]
    public async Task XirrWithCredits_ReadsTheCreditsBearingSeriesSeparatelyFromXirr()
    {
        var navigationService = new FakeNavigationService
        {
            AssetDetails = BuildAssetDetails(totalCredits: 50m, priceOnlyReturn: 0.05m, totalReturn: 0.0513m),
        };
        var vm = BuildViewModel(new FixedPriceService(1000m), navigationService: navigationService);

        vm.LoadAssetDetails(BuildAssetDetails(totalCredits: 50m));
        await vm.EnsureTodayInfoLoadedAsync();

        vm.Xirr.Should().Be(0.05m);
        vm.XirrWithCredits.Should().Be(0.0513m);
        vm.XirrWithCredits!.Value.Should().BeGreaterThan(vm.Xirr!.Value, "a credit received improves the rate");
    }

    [Fact]
    public async Task TotalCurrentValueWithCredits_AddsTotalCreditsToMarketValue()
    {
        var navigationService = new FakeNavigationService
        {
            AssetDetails = BuildAssetDetails(totalCredits: 50m, marketValue: 1000m),
        };
        var vm = BuildViewModel(new FixedPriceService(1000m), navigationService: navigationService);

        vm.LoadAssetDetails(BuildAssetDetails(totalCredits: 50m));
        await vm.EnsureTodayInfoLoadedAsync();

        vm.TotalCurrentValue.Should().Be(1000m);
        vm.TotalCurrentValueWithCredits.Should().Be(1050m);
    }

    [Fact]
    public void Clear_ResetsXirrToNull()
    {
        var vm = BuildViewModel(new FixedPriceService(1100m));
        vm.LoadAssetDetails(BuildAssetDetails(priceOnlyReturn: 0.10m, totalReturn: 0.10m));

        vm.Clear();

        vm.Xirr.Should().BeNull();
        vm.XirrWithCredits.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_SetsRealizedGainLossFromDto()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails(realizedGainLoss: 62m));

        vm.RealizedGainLoss.Should().Be(62m);
    }

    [Fact]
    public void Clear_ResetsRealizedGainLossToZero()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails(realizedGainLoss: 62m));

        vm.Clear();

        vm.RealizedGainLoss.Should().Be(0m);
    }

    [Fact]
    public void RealizedXirr_BeforeAssetLoaded_IsNull()
    {
        var vm = BuildViewModel();

        vm.RealizedXirr.Should().BeNull();
        vm.RealizedXirrWithCredits.Should().BeNull();
    }

    [Fact]
    public void RealizedXirr_ReadsPriceOnlyReturnFromDto()
    {
        // Historic's market value is a concrete zero (not unavailable), so the server always
        // solves PriceOnlyReturn/TotalReturn against it - RealizedXirr is the same field Xirr
        // reads, not a second client-side calculation.
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        vm.LoadAssetDetails(BuildAssetDetails(priceOnlyReturn: 0.10m));

        vm.RealizedXirr.Should().Be(0.10m);
    }

    [Fact]
    public void RealizedXirrWithCredits_ReadsTotalReturnFromDto()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        vm.LoadAssetDetails(BuildAssetDetails(totalCredits: 50m, priceOnlyReturn: 0.10m, totalReturn: 0.12m));

        vm.RealizedXirr.Should().Be(0.10m);
        vm.RealizedXirrWithCredits.Should().Be(0.12m);
        vm.RealizedXirrWithCredits!.Value.Should().BeGreaterThan(vm.RealizedXirr!.Value);
    }

    [Fact]
    public void Clear_ResetsRealizedXirrToNull()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);
        vm.LoadAssetDetails(BuildAssetDetails(priceOnlyReturn: 0.10m, totalReturn: 0.10m));

        vm.Clear();

        vm.RealizedXirr.Should().BeNull();
        vm.RealizedXirrWithCredits.Should().BeNull();
    }

    [Fact]
    public void LoadAssetDetails_SetsRealizedPortfolioWeightFromParameter()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails(), realizedPortfolioWeight: 5.15m);

        vm.RealizedPortfolioWeight.Should().Be(5.15m);
        vm.DisplayRealizedPortfolioWeight.Should().Be("5.15%");
    }

    [Fact]
    public void LoadAssetDetails_DefaultsRealizedPortfolioWeightToNull_WhenNotProvided()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails());

        vm.RealizedPortfolioWeight.Should().BeNull();
        vm.DisplayRealizedPortfolioWeight.Should().Be("—");
    }

    [Fact]
    public void Clear_ResetsRealizedPortfolioWeightToNull()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails(), realizedPortfolioWeight: 5.15m);

        vm.Clear();

        vm.RealizedPortfolioWeight.Should().BeNull();
        vm.DisplayRealizedPortfolioWeight.Should().Be("—");
    }

    [Fact]
    public void IsHistoricScope_WhenScopeIsHistoric_IsTrue()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Historic);

        vm.IsHistoricScope.Should().BeTrue();
        vm.IsActiveScope.Should().BeFalse();
    }

    [Fact]
    public void IsHistoricScope_WhenScopeIsActive_IsFalse()
    {
        var vm = BuildViewModel(scope: InvestmentScope.Active);

        vm.IsHistoricScope.Should().BeFalse();
        vm.IsActiveScope.Should().BeTrue();
    }

    private sealed class FixedPriceService : IAssetPriceLookupService
    {
        private readonly decimal _price;

        public int CallCount { get; private set; }

        public FixedPriceService(decimal price)
        {
            _price = price;
        }

        public Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request)
        {
            CallCount++;
            return Task.FromResult(new AssetPriceDTO { Exchange = request.Exchange, Ticker = request.Ticker, Price = _price, AsOf = DateTimeOffset.UtcNow });
        }
    }

    private sealed class NotUsedAssetPriceService : IAssetPriceService
    {
        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) => throw new NotImplementedException();
    }
}
