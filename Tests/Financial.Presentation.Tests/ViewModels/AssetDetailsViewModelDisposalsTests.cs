using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelDisposalsTests
{
    private static AssetDetailsViewModel BuildViewModel()
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new FakeNavigationService(),
            new FakePortfolioAssetSummaryService(),
            new ProfitCalculationService());
    }

    private static DisposalRecordDTO CreateRecord(string taxYear) => new()
    {
        Id = Guid.NewGuid(),
        TransactionId = Guid.NewGuid(),
        Date = new DateTime(2025, 6, 1),
        Method = CostBasisMethod.AverageCost,
        LotsConsumed = new List<DisposalLotConsumptionDTO>(),
        QuantityDisposed = 5m,
        Proceeds = 50m,
        CostBasis = 40m,
        GainLoss = 10m,
        Currency = "GBP",
        TaxYear = taxYear,
        Status = DisposalRecordStatus.Active,
        SupersededByRecordId = null,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static AssetDetailsDTO BuildAssetDetails(IReadOnlyList<DisposalRecordDTO> disposalRecords) => new()
    {
        Name = "TEST",
        BrokerName = "XPI",
        PortfolioName = "Default",
        Ticker = "TEST",
        Exchange = "BVMF",
        DisposalRecords = disposalRecords.ToList()
    };

    [Fact]
    public void LoadAssetDetails_ForwardsDisposalRecordsToDisposalsTab()
    {
        var vm = BuildViewModel();
        var record = CreateRecord("2025/26");

        vm.LoadAssetDetails(BuildAssetDetails([record]));

        vm.Disposals.Disposals.Should().ContainSingle(row => row.Record == record);
    }

    [Fact]
    public void LoadAssetDetails_WithNoDisposals_ShowsInitialEmptyState()
    {
        var vm = BuildViewModel();

        vm.LoadAssetDetails(BuildAssetDetails([]));

        vm.Disposals.HasNoDisposalsAtAll.Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsDisposalsTab()
    {
        var vm = BuildViewModel();
        vm.LoadAssetDetails(BuildAssetDetails([CreateRecord("2025/26")]));

        vm.Clear();

        vm.Disposals.Disposals.Should().BeEmpty();
        vm.Disposals.HasNoDisposalsAtAll.Should().BeTrue();
    }
}
