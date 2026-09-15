using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class DisposalsTabViewModelTests
{
    private static DisposalRecordDTO CreateRecord(
        DateTime date,
        string taxYear,
        decimal quantity = 10m,
        DisposalRecordStatus status = DisposalRecordStatus.Active,
        Guid? id = null,
        Guid? transactionId = null,
        Guid? supersededByRecordId = null)
    {
        return new DisposalRecordDTO
        {
            Id = id ?? Guid.NewGuid(),
            TransactionId = transactionId ?? Guid.NewGuid(),
            Date = date,
            Method = CostBasisMethod.AverageCost,
            LotsConsumed = new List<DisposalLotConsumptionDTO>(),
            QuantityDisposed = quantity,
            Proceeds = 100m,
            CostBasis = 80m,
            GainLoss = 20m,
            Currency = "GBP",
            TaxYear = taxYear,
            Status = status,
            SupersededByRecordId = supersededByRecordId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public void Load_FiltersToActiveOnly_AndSortsNewestFirst()
    {
        var viewModel = new DisposalsTabViewModel();
        var older = CreateRecord(new DateTime(2025, 1, 1), "2024/25");
        var newer = CreateRecord(new DateTime(2025, 6, 1), "2025/26");
        var superseded = CreateRecord(new DateTime(2025, 3, 1), "2024/25", status: DisposalRecordStatus.Superseded);

        viewModel.Load("asset-1", [older, newer, superseded]);

        viewModel.Disposals.Should().HaveCount(2);
        viewModel.Disposals[0].Record.Should().Be(newer);
        viewModel.Disposals[1].Record.Should().Be(older);
    }

    [Fact]
    public void Load_DerivesTaxYearOptionsFromPresentData_WithAllFirst()
    {
        var viewModel = new DisposalsTabViewModel();
        var recordA = CreateRecord(new DateTime(2025, 1, 1), "2024/25");
        var recordB = CreateRecord(new DateTime(2025, 6, 1), "2025/26");

        viewModel.Load("asset-1", [recordA, recordB]);

        viewModel.TaxYearFilters.Select(f => f.Label).Should().Equal("All", "2025/26", "2024/25");
        viewModel.TaxYearFilters[0].IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectTaxYearCommand_FiltersDisposalsToSelectedTaxYear()
    {
        var viewModel = new DisposalsTabViewModel();
        var recordA = CreateRecord(new DateTime(2025, 1, 1), "2024/25");
        var recordB = CreateRecord(new DateTime(2025, 6, 1), "2025/26");
        viewModel.Load("asset-1", [recordA, recordB]);

        var option = viewModel.TaxYearFilters.Single(f => f.Label == "2024/25");
        viewModel.SelectTaxYearCommand.Execute(option);

        viewModel.Disposals.Should().ContainSingle(row => row.Record == recordA);
        viewModel.HasNoDisposalsInSelectedTaxYear.Should().BeFalse();
    }

    [Fact]
    public void Load_PerAssetViewState_RestoresEachAssetsOwnTaxYearSelection()
    {
        var viewModel = new DisposalsTabViewModel();
        var assetARecords = new List<DisposalRecordDTO> { CreateRecord(new DateTime(2025, 1, 1), "2024/25") };
        var assetBRecords = new List<DisposalRecordDTO> { CreateRecord(new DateTime(2025, 6, 1), "2025/26") };

        viewModel.Load("asset-a", assetARecords);
        viewModel.SelectTaxYearCommand.Execute(viewModel.TaxYearFilters.Single(f => f.Label == "2024/25"));

        viewModel.Load("asset-b", assetBRecords);
        viewModel.TaxYearFilters.Single(f => f.Label == "All").IsSelected.Should().BeTrue();

        viewModel.Load("asset-a", assetARecords);
        viewModel.TaxYearFilters.Single(f => f.Label == "2024/25").IsSelected.Should().BeTrue();
    }

    [Fact]
    public void Load_ResolvesSupersededHistoryForAChainedDisposal()
    {
        var viewModel = new DisposalsTabViewModel();
        var transactionId = Guid.NewGuid();
        var oldestId = Guid.NewGuid();
        var middleId = Guid.NewGuid();
        var activeId = Guid.NewGuid();

        var oldest = CreateRecord(new DateTime(2025, 1, 1), "2024/25", transactionId: transactionId,
            status: DisposalRecordStatus.Superseded, id: oldestId, supersededByRecordId: middleId);
        var middle = CreateRecord(new DateTime(2025, 1, 1), "2024/25", transactionId: transactionId,
            status: DisposalRecordStatus.Superseded, id: middleId, supersededByRecordId: activeId);
        var active = CreateRecord(new DateTime(2025, 1, 1), "2024/25", transactionId: transactionId,
            status: DisposalRecordStatus.Active, id: activeId);

        viewModel.Load("asset-1", [oldest, middle, active]);

        var row = viewModel.Disposals.Should().ContainSingle().Subject;
        row.Record.Should().Be(active);
        row.HasHistory.Should().BeTrue();
        row.SupersededHistory.Should().Equal(oldest, middle);
    }

    [Fact]
    public void Load_WithNoActiveRecords_SetsHasNoDisposalsAtAll()
    {
        var viewModel = new DisposalsTabViewModel();

        viewModel.Load("asset-1", []);

        viewModel.HasNoDisposalsAtAll.Should().BeTrue();
        viewModel.HasAnyDisposals.Should().BeFalse();
        viewModel.Disposals.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ResetsAllState()
    {
        var viewModel = new DisposalsTabViewModel();
        viewModel.Load("asset-1", [CreateRecord(new DateTime(2025, 1, 1), "2024/25")]);

        viewModel.Clear();

        viewModel.Disposals.Should().BeEmpty();
        viewModel.TaxYearFilters.Should().BeEmpty();
        viewModel.HasNoDisposalsAtAll.Should().BeTrue();
        viewModel.HasVisibleDisposals.Should().BeFalse();
    }
}
