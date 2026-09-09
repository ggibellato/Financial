using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelCoverageTests
{
    private static AssetDetailsViewModel BuildViewModel(
        ITransactionService? transactionService = null,
        ICreditService? creditService = null,
        IAssetPriceHistoryService? priceHistoryService = null,
        IAssetPriceLookupService? priceLookupService = null,
        InvestmentScope scope = InvestmentScope.Active) =>
        new(
            transactionService ?? new StubTransactionService(),
            creditService ?? new StubCreditService(),
            new NotUsedAssetPriceService(),
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new XirrCalculationService(),
            new ProfitCalculationService(),
            scope,
            priceLookupService,
            priceHistoryService);

    private static AssetDetailsDTO BuildAssetDetails(
        decimal quantity = 10m,
        decimal averagePrice = 100m,
        decimal totalCredits = 50m) => new()
    {
        Name = "TEST",
        BrokerName = "XPI",
        PortfolioName = "Default",
        Ticker = "TEST",
        Quantity = quantity,
        AveragePrice = averagePrice,
        TotalCredits = totalCredits
    };

    [Fact]
    public void ComputedProperties_AfterLoadAssetDetails_AreReadableAndConsistent()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails());

        vm.HasCreditsContext.Should().BeTrue();
        vm.IsCreditsAssetView.Should().BeTrue();
        vm.ShouldShowEmptyState.Should().BeFalse();
        vm.HasAveragePrice.Should().BeTrue();
        vm.ResultPercent.Should().Be(vm.ResultPercent);
        vm.ResultPercentWithCredits.Should().Be(vm.ResultPercentWithCredits);
        vm.HasBreakdownError.Should().BeFalse();
    }

    [Fact]
    public void RefreshTodayInfoCommandAndCopyAssetNameCommand_CanExecute_ReflectAssetContext()
    {
        var vm = BuildViewModel();

        vm.RefreshTodayInfoCommand.CanExecute(null).Should().BeFalse();
        vm.CopyAssetNameCommand.CanExecute(null).Should().BeFalse();

        vm.LoadAssetDetails(BuildAssetDetails());

        vm.RefreshTodayInfoCommand.CanExecute(null).Should().BeTrue();
        vm.CopyAssetNameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RefreshTodayInfoCommand_Execute_WithoutAssetContext_SetsSelectAssetMessage()
    {
        var vm = BuildViewModel();

        vm.RefreshTodayInfoCommand.Execute(null);

        vm.TodayInfoMessage.Should().Be("Select an asset to load current values.");
    }

    [Fact]
    public void LoadPortfolioCredits_DelegatesToAggregateCreditsAndSetsPortfolioTotals()
    {
        var vm = BuildViewModel();
        var summary = new AggregatedSummaryDTO { TotalBought = 500m, TotalSold = 100m };
        var credits = new List<CreditDTO> { new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 25m } };

        vm.LoadPortfolioCredits("Broker", "Portfolio", summary, credits);

        vm.TotalBought.Should().Be(500m);
        vm.TotalSold.Should().Be(100m);
        vm.HasCreditsContext.Should().BeTrue();
        vm.Credits.Credits.Should().ContainSingle();
    }

    [Fact]
    public async Task LoadPortfolioSummary_WithoutPriceLookupService_MarksRowsAsPriceFailed()
    {
        var vm = BuildViewModel(priceLookupService: null);
        var items = new List<PortfolioAssetSummaryItemDTO>
        {
            new() { AssetName = "Asset 1", Ticker = "T1", Exchange = "LSE", CurrentQuantity = 1m, TotalBought = 10m, TotalSold = 0m, TotalInvested = 10m, PortfolioWeight = 100m }
        };

        vm.LoadPortfolioSummary("Broker", "Portfolio", new AggregatedSummaryDTO(), [], items);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (vm.PortfolioAssetSummaryRows.Any(r => r.IsLoadingPrice) && DateTime.UtcNow < deadline)
            await Task.Delay(25);

        vm.PortfolioAssetSummaryRows.Should().OnlyContain(r => !r.IsLoadingPrice);
    }

    [Fact]
    public async Task Transactions_DeleteThroughParentContext_UsesBrokerPortfolioAssetLambdasAndAppliesDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = "TEST", BrokerName = "XPI", PortfolioName = "Default", Ticker = "TEST" };
        var transactionService = new ConfigurableTransactionService { DeleteResult = expectedDetails };
        var vm = BuildViewModel(transactionService: transactionService);
        vm.LoadAssetDetails(BuildAssetDetails());
        var tx = new TransactionDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Buy", Quantity = 1m, UnitPrice = 1m, Fees = 0m };

        vm.Transactions.DeleteTransactionCommand.CanExecute(tx).Should().BeTrue();
        await vm.Transactions.Delete(tx, () => true);

        transactionService.LastDeleteRequest.Should().NotBeNull();
        transactionService.LastDeleteRequest!.BrokerName.Should().Be("XPI");
        transactionService.LastDeleteRequest.PortfolioName.Should().Be("Default");
        transactionService.LastDeleteRequest.AssetName.Should().Be("TEST");
        vm.AveragePrice.Should().Be(expectedDetails.AveragePrice);
    }

    [Fact]
    public async Task Credits_DeleteThroughParentContext_UsesBrokerPortfolioAssetLambdasAndAppliesDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = "TEST", BrokerName = "XPI", PortfolioName = "Default", Ticker = "TEST" };
        var creditService = new ConfigurableCreditService { DeleteResult = expectedDetails };
        var vm = BuildViewModel(creditService: creditService);
        vm.LoadAssetDetails(BuildAssetDetails());
        var credit = new CreditDTO { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 10m };

        vm.Credits.DeleteCreditCommand.CanExecute(credit).Should().BeTrue();
        await vm.Credits.Delete(credit, () => true);

        creditService.LastDeleteRequest.Should().NotBeNull();
        creditService.LastDeleteRequest!.BrokerName.Should().Be("XPI");
        creditService.LastDeleteRequest.PortfolioName.Should().Be("Default");
        creditService.LastDeleteRequest.AssetName.Should().Be("TEST");
        vm.AveragePrice.Should().Be(expectedDetails.AveragePrice);
    }

    [Fact]
    public async Task PriceHistory_DeleteThroughParentContext_UsesBrokerPortfolioAssetLambdasAndAppliesDetails()
    {
        var expectedDetails = new AssetDetailsDTO { Name = "TEST", BrokerName = "XPI", PortfolioName = "Default", Ticker = "TEST" };
        var priceHistoryService = new ConfigurablePriceHistoryService { DeleteResult = expectedDetails };
        var vm = BuildViewModel(priceHistoryService: priceHistoryService);
        vm.LoadAssetDetails(BuildAssetDetails());
        var entry = new AssetPriceSnapshotDTO { Date = DateOnly.FromDateTime(DateTime.Today), Price = 10m, IsManual = true };

        vm.PriceHistory.DeletePriceCommand.CanExecute(entry).Should().BeTrue();
        await vm.PriceHistory.Delete(entry, () => true);

        priceHistoryService.LastDeleteRequest.Should().NotBeNull();
        priceHistoryService.LastDeleteRequest!.BrokerName.Should().Be("XPI");
        priceHistoryService.LastDeleteRequest.PortfolioName.Should().Be("Default");
        priceHistoryService.LastDeleteRequest.AssetName.Should().Be("TEST");
        vm.AveragePrice.Should().Be(expectedDetails.AveragePrice);
    }

    private sealed class NotUsedAssetPriceService : IAssetPriceService
    {
        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) => throw new NotImplementedException();
    }

    private sealed class ConfigurableTransactionService : ITransactionService
    {
        public AssetDetailsDTO? DeleteResult { get; set; }
        public TransactionDeleteDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);

        public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request)
        {
            LastDeleteRequest = request;
            return Task.FromResult(DeleteResult);
        }
    }

    private sealed class ConfigurableCreditService : ICreditService
    {
        public AssetDetailsDTO? DeleteResult { get; set; }
        public CreditDeleteDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);
        public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request) => Task.FromResult<AssetDetailsDTO?>(null);

        public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request)
        {
            LastDeleteRequest = request;
            return Task.FromResult(DeleteResult);
        }
    }

    private sealed class ConfigurablePriceHistoryService : IAssetPriceHistoryService
    {
        public AssetDetailsDTO? DeleteResult { get; set; }
        public DeleteAssetPriceDTO? LastDeleteRequest { get; private set; }

        public Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request) => Task.FromResult<AssetDetailsDTO?>(null);

        public Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request)
        {
            LastDeleteRequest = request;
            return Task.FromResult(DeleteResult);
        }
    }
}
