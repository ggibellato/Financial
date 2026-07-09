using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelPortfolioSummaryTests
{
    private static AssetDetailsViewModel BuildViewModel(IAssetPriceService? priceService = null)
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            priceService ?? new NeverResolvingPriceService(),
            new StubBrokerBreakdownQueryService());
    }

    private static IReadOnlyList<PortfolioAssetSummaryItemDTO> BuildItems(int count = 2)
    {
        return Enumerable.Range(1, count).Select(i => new PortfolioAssetSummaryItemDTO
        {
            AssetName = $"Asset {i}",
            Ticker = $"T{i}",
            Exchange = "LSE",
            CurrentQuantity = 10m * i,
            TotalBought = 1000m * i,
            TotalSold = 0m,
            TotalInvested = 1000m * i,
            PortfolioWeight = 50m
        }).ToList();
    }

    private static PortfolioAssetSummaryItemDTO BuildItem(
        string assetName = "Asset",
        decimal currentQuantity = 10m,
        decimal totalInvested = 1000m,
        decimal totalCredits = 0m,
        decimal currentMonthCredits = 0m,
        decimal? estimatedAnnualCredits = null)
    {
        return new PortfolioAssetSummaryItemDTO
        {
            AssetName = assetName,
            Ticker = "T",
            Exchange = "LSE",
            CurrentQuantity = currentQuantity,
            TotalBought = totalInvested,
            TotalSold = 0m,
            TotalInvested = totalInvested,
            PortfolioWeight = 50m,
            TotalCredits = totalCredits,
            CurrentMonthCredits = currentMonthCredits,
            EstimatedAnnualCredits = estimatedAnnualCredits
        };
    }

    [Fact]
    public void LoadPortfolioSummary_SetsIsPortfolioViewTrue()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems());
        vm.IsPortfolioView.Should().BeTrue();
    }

    [Fact]
    public void LoadPortfolioSummary_PopulatesPortfolioAssetSummaryRows()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(2));
        vm.PortfolioAssetSummaryRows.Count.Should().Be(2);
    }

    [Fact]
    public void LoadPortfolioSummary_RowsAreInCorrectOrder()
    {
        var vm = BuildViewModel();
        var items = BuildItems(2);
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.PortfolioAssetSummaryRows[0].AssetName.Should().Be(items[0].AssetName);
        vm.PortfolioAssetSummaryRows[1].AssetName.Should().Be(items[1].AssetName);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsAggregatedTotals()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 10000m, TotalSold = 2000m };
        vm.LoadPortfolioSummary("Broker", "Portfolio", summary, [], BuildItems());
        vm.TotalBought.Should().Be(10000m);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsTotalInvested()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 10000m, TotalSold = 2000m, TotalInvested = 8000m };
        vm.LoadPortfolioSummary("Broker", "Portfolio", summary, [], BuildItems());
        vm.TotalInvested.Should().Be(8000m);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsNegativeTotalInvested()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 1000m, TotalSold = 3000m, TotalInvested = -2000m };
        vm.LoadPortfolioSummary("Broker", "Portfolio", summary, [], BuildItems());
        vm.TotalInvested.Should().Be(-2000m);
    }

    [Fact]
    public void LoadPortfolioSummary_LoadsCreditsForCreditsTab()
    {
        var vm = BuildViewModel();
        var credits = new List<CreditDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 100m },
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Interest", Value = 50m }
        };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), credits, BuildItems());
        vm.Credits.Count.Should().Be(2);
    }

    [Fact]
    public void LoadPortfolioSummary_RowsInitiallyShowLoadingPrice()
    {
        var vm = BuildViewModel(new NeverResolvingPriceService());
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(3));
        vm.PortfolioAssetSummaryRows.All(r => r.IsLoadingPrice).Should().BeTrue();
    }

    [Fact]
    public void Clear_AfterLoadPortfolioSummary_ClearsRows()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(2));
        vm.Clear();
        vm.PortfolioAssetSummaryRows.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_AfterLoadPortfolioSummary_SetsIsPortfolioViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems());
        vm.Clear();
        vm.IsPortfolioView.Should().BeFalse();
    }

    [Fact]
    public void Clear_AfterLoadPortfolioSummary_ResetsTotalInvested()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 10000m, TotalSold = 2000m, TotalInvested = 8000m };
        vm.LoadPortfolioSummary("Broker", "Portfolio", summary, [], BuildItems());
        vm.Clear();
        vm.TotalInvested.Should().Be(0m);
    }

    [Fact]
    public void LoadAssetDetails_AfterPortfolioSummary_SetsIsPortfolioViewFalse()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems());
        vm.LoadAssetDetails(new AssetDetailsDTO
        {
            Name = "Asset A", BrokerName = "Broker", PortfolioName = "Portfolio",
            Ticker = "T", ISIN = "", Exchange = "LSE",
            Country = Financial.Domain.Entities.CountryCode.Unknown,
            LocalTypeCode = "", Class = Financial.Domain.Entities.GlobalAssetClass.Unknown,
            Quantity = 0m, AveragePrice = 0m, IsActive = true,
            TotalBought = 0m, TotalSold = 0m, TotalCredits = 0m,
            Transactions = [], Credits = []
        });
        vm.IsPortfolioView.Should().BeFalse();
    }

    [Fact]
    public void LoadPortfolioSummary_SetsFooterTotalInvested_SumOfRows()
    {
        var vm = BuildViewModel();
        var items = new[] { BuildItem(totalInvested: 1000m), BuildItem(totalInvested: 2000m) };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.FooterTotalInvested.Should().Be(3000m);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsFooterTotalCredits_SumOfRows()
    {
        var vm = BuildViewModel();
        var items = new[] { BuildItem(totalCredits: 50m), BuildItem(totalCredits: 75m) };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.FooterTotalCredits.Should().Be(125m);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsFooterCurrentMonthCredits_SumOfRows()
    {
        var vm = BuildViewModel();
        var items = new[] { BuildItem(currentMonthCredits: 10m), BuildItem(currentMonthCredits: 20m) };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.FooterCurrentMonthCredits.Should().Be(30m);
    }

    [Fact]
    public void LoadPortfolioSummary_SetsFooterCurrentMonthLabel_IncludesCreditsAndMonthYear()
    {
        var vm = BuildViewModel();
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(1));
        vm.FooterCurrentMonthLabel.Should().StartWith("Credits ");
        vm.FooterCurrentMonthLabel.Should().MatchRegex(@"Credits [A-Z][a-z]{2} \d{4}");
    }

    [Fact]
    public void LoadPortfolioSummary_SetsFooterEstimatedAnnualCreditsDisplay_SumOfNonNull()
    {
        var vm = BuildViewModel();
        var items = new[] { BuildItem(estimatedAnnualCredits: 600m), BuildItem(estimatedAnnualCredits: null) };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.FooterEstimatedAnnualCreditsDisplay.Should().Be("600.00");
    }

    [Fact]
    public void LoadPortfolioSummary_SetsFooterEstimatedAnnualCreditsDisplay_DashWhenAllNull()
    {
        var vm = BuildViewModel();
        var items = new[] { BuildItem(estimatedAnnualCredits: null), BuildItem(estimatedAnnualCredits: null) };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);
        vm.FooterEstimatedAnnualCreditsDisplay.Should().Be("—");
    }

    [Fact]
    public void FooterCurrentValueDisplay_WhenAnyRowIsLoading_ReturnsCalculating()
    {
        var vm = BuildViewModel(new NeverResolvingPriceService());
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], BuildItems(2));
        vm.FooterCurrentValueDisplay.Should().Be("Calculating…");
    }

    [Fact]
    public void FooterCurrentValueDisplay_WhenAllPricesResolved_ReturnsSumN2()
    {
        var vm = BuildViewModel(new NeverResolvingPriceService());
        var items = new[]
        {
            BuildItem(currentQuantity: 5m),
            BuildItem(currentQuantity: 2m)
        };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);

        vm.PortfolioAssetSummaryRows[0].ApplyPrice(10m);  // CV = 50
        vm.PortfolioAssetSummaryRows[1].ApplyPrice(20m);  // CV = 40

        vm.FooterCurrentValueDisplay.Should().Be("90.00");
    }

    [Fact]
    public void Clear_AfterLoadPortfolioSummary_ResetsFooterProperties()
    {
        var vm = BuildViewModel();
        var items = new[] { BuildItem(totalInvested: 1000m, totalCredits: 50m, currentMonthCredits: 10m, estimatedAnnualCredits: 600m) };
        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);

        vm.Clear();

        vm.FooterTotalInvested.Should().Be(0m);
        vm.FooterTotalCredits.Should().Be(0m);
        vm.FooterCurrentMonthCredits.Should().Be(0m);
        vm.FooterCurrentMonthLabel.Should().Be(string.Empty);
        vm.FooterEstimatedAnnualCreditsDisplay.Should().Be("—");
    }

    private sealed class StubBrokerBreakdownQueryService : IBrokerBreakdownQueryService
    {
        public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName) => [];
    }

    private sealed class NeverResolvingPriceService : IAssetPriceService
    {
        private readonly SemaphoreSlim _blocker = new SemaphoreSlim(0);

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            _blocker.Wait();
            return new AssetPriceDTO { Exchange = request.Exchange, Ticker = request.Ticker, Price = 0m };
        }
    }

    private sealed class StubTransactionService : ITransactionService
    {
        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    }

    private sealed class StubCreditService : ICreditService
    {
        public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
    }
}
