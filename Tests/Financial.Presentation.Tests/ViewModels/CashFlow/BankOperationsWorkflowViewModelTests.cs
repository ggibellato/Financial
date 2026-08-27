using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

/// <summary>
/// Covers the Bank tab's flat, cross-bank operations list and its bank filter.
/// </summary>
public class BankOperationsWorkflowViewModelTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static readonly List<BankDTO> DefaultBanks =
    [
        new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
        new() { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
    ];

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Unchecks every filter option except the given values, mirroring how a user would
    /// narrow the header checklist down to a single bank.</summary>
    private static void SelectOnly(ColumnFilterViewModel<BankOperationRow> filter, params string[] values)
    {
        foreach (var option in filter.Options)
        {
            option.IsChecked = values.Contains(option.Value);
        }
    }

    private static (BankOperationsWorkflowViewModel ViewModel, StubTransferService Transfers, StubBalanceAdjustmentService Adjustments, Func<int> RefreshCallCount) CreateViewModel(bool confirmDeletes = true)
    {
        var transferService = new StubTransferService();
        var adjustmentService = new StubBalanceAdjustmentService();
        var banks = new ObservableCollection<BankDTO>();
        var bankTotals = new ObservableCollection<BankTotalRow>();
        var refreshCount = 0;
        var viewModel = new BankOperationsWorkflowViewModel(
            transferService, adjustmentService, banks, bankTotals,
            confirm: _ => confirmDeletes,
            refresh: () => { refreshCount++; return Task.CompletedTask; });
        return (viewModel, transferService, adjustmentService, () => refreshCount);
    }

    [Fact]
    public void BuildBankOperations_CombinesTransfersAndAdjustments_SortedNewestFirst()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<TransferDTO> transfers = [new() { Id = Guid.NewGuid(), Date = Today.AddDays(-1), SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m }];
        List<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank = [[new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }]];

        viewModel.ApplyRefresh(transfers, adjustmentsByBank, Today.Year, Today.Month, DefaultBanks);

        viewModel.BankOperations.Should().HaveCount(2);
        viewModel.BankOperations[0].Kind.Should().Be(BankOperationKind.Adjustment);
        viewModel.BankOperations[1].Kind.Should().Be(BankOperationKind.Transfer);
    }

    [Fact]
    public void BuildBankOperations_TransferRow_ShowsSourceArrowDestinationLabel()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<TransferDTO> transfers = [new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m }];

        viewModel.ApplyRefresh(transfers, [], Today.Year, Today.Month, DefaultBanks);

        var row = viewModel.BankOperations.Single();
        row.BankLabel.Should().Be("Barclays → Chase");
        row.Kind.Should().Be(BankOperationKind.Transfer);
        row.DisplayAmount.Should().Be(50m);
    }

    [Fact]
    public void BuildBankOperations_AdjustmentRow_ShowsSingleBankLabelAndSignedDelta()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank = [[new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = -12.5m }]];

        viewModel.ApplyRefresh([], adjustmentsByBank, Today.Year, Today.Month, DefaultBanks);

        var row = viewModel.BankOperations.Single();
        row.BankLabel.Should().Be("Barclays");
        row.Kind.Should().Be(BankOperationKind.Adjustment);
        row.DisplayAmount.Should().Be(-12.5m);
    }

    [Fact]
    public void BuildBankOperations_AdjustmentOutsideSelectedMonth_Excluded()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank = [[new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today.AddMonths(-2), BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }]];

        viewModel.ApplyRefresh([], adjustmentsByBank, Today.Year, Today.Month, DefaultBanks);

        viewModel.BankOperations.Should().BeEmpty();
    }

    [Fact]
    public void BuildBankOperations_DuplicateBankNames_DoesNotThrow()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        var duplicateId = Guid.NewGuid();
        var banks = new List<BankDTO>(DefaultBanks) { new() { Id = duplicateId, Name = "Barclays", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = Today } };
        List<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank =
        [
            [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }],
            [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = duplicateId, BankName = "Barclays", TargetBalance = 50m, Delta = -1m }],
        ];

        var act = () => viewModel.ApplyRefresh([], adjustmentsByBank, Today.Year, Today.Month, banks);

        act.Should().NotThrow();
        viewModel.BankOperations.Should().HaveCount(2);
    }

    [Fact]
    public void BankFilter_DefaultsToUnfiltered_ShowsEveryRow()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<TransferDTO> transfers = [new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m }];
        List<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank = [[new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = ChaseId, BankName = "Chase", TargetBalance = 100m, Delta = 5m }]];

        viewModel.ApplyRefresh(transfers, adjustmentsByBank, Today.Year, Today.Month, DefaultBanks);

        viewModel.BankFilter.IsFiltered.Should().BeFalse();
        viewModel.FilteredBankOperations.Count.Should().Be(viewModel.BankOperations.Count);
    }

    [Fact]
    public void BankFilter_SelectingBank_MatchesTransferAsSourceOrDestination()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<TransferDTO> transfers =
        [
            new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 10m },
            new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = ChaseId, SourceBankName = "Chase", DestinationBankId = BarclaysId, DestinationBankName = "Barclays", Amount = 20m },
        ];
        viewModel.ApplyRefresh(transfers, [], Today.Year, Today.Month, DefaultBanks);

        SelectOnly(viewModel.BankFilter, "Barclays");

        viewModel.FilteredBankOperations.Should().HaveCount(2);
        viewModel.FilteredBankOperations.Should().OnlyContain(r => r.SourceBank == "Barclays" || r.DestinationBank == "Barclays");
    }

    [Fact]
    public void BankFilter_SelectingBank_MatchesAdjustmentExactBankOnly()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank =
        [
            [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }],
            [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = ChaseId, BankName = "Chase", TargetBalance = 50m, Delta = -1m }],
        ];
        viewModel.ApplyRefresh([], adjustmentsByBank, Today.Year, Today.Month, DefaultBanks);

        SelectOnly(viewModel.BankFilter, "Barclays");

        var row = viewModel.FilteredBankOperations.Single();
        row.Bank.Should().Be("Barclays");
    }

    [Fact]
    public void BankFilter_ToggleAll_RestoresFullList()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<TransferDTO> transfers = [new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 10m }];
        viewModel.ApplyRefresh(transfers, [], Today.Year, Today.Month, DefaultBanks);

        SelectOnly(viewModel.BankFilter, "Chase");
        viewModel.FilteredBankOperations.Should().HaveCount(1);

        viewModel.BankFilter.ToggleAllCommand.Execute(null);

        viewModel.FilteredBankOperations.Count.Should().Be(viewModel.BankOperations.Count);
    }

    [Fact]
    public void BankFilter_ChangingSelection_DoesNotTriggerRefresh()
    {
        var (viewModel, _, _, refreshCallCount) = CreateViewModel();
        List<TransferDTO> transfers = [new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 10m }];
        viewModel.ApplyRefresh(transfers, [], Today.Year, Today.Month, DefaultBanks);
        var callsBefore = refreshCallCount();

        SelectOnly(viewModel.BankFilter, "Chase");
        SelectOnly(viewModel.BankFilter, "Barclays");
        viewModel.BankFilter.ToggleAllCommand.Execute(null);

        refreshCallCount().Should().Be(callsBefore);
    }

    [Fact]
    public async Task DeleteBankOperation_Transfer_ConfirmedCallsTransferDelete()
    {
        var (viewModel, transfers, _, _) = CreateViewModel();
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m };
        var row = BankOperationRow.FromTransfer(transfer);

        await viewModel.DeleteBankOperationAsync(row);

        transfers.LastDeletedId.Should().Be(transfer.Id);
    }

    [Fact]
    public async Task DeleteBankOperation_Adjustment_ConfirmedCallsAdjustmentDelete()
    {
        var (viewModel, _, adjustments, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };
        var row = BankOperationRow.FromAdjustment(adjustment);

        await viewModel.DeleteBankOperationAsync(row);

        adjustments.LastDeleted.Should().Be((adjustment.BankId, adjustment.Id));
    }

    [Fact]
    public async Task DeleteBankOperation_Declined_SkipsService()
    {
        var (viewModel, transfers, adjustments, _) = CreateViewModel(confirmDeletes: false);
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m };
        var row = BankOperationRow.FromTransfer(transfer);

        await viewModel.DeleteBankOperationAsync(row);

        transfers.LastDeletedId.Should().BeNull();
        adjustments.LastDeleted.Should().BeNull();
    }

    [Fact]
    public void BankOperationsEmptyMessage_NoOperationsAtAll_ShowsGenericMessage()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        viewModel.ApplyRefresh([], [], Today.Year, Today.Month, DefaultBanks);

        viewModel.HasAnyBankOperationsBeforeFilter.Should().BeFalse();
        viewModel.HasBankOperations.Should().BeFalse();
        viewModel.BankOperationsEmptyMessage.Should().Be("No transfers or balance corrections this month.");
    }

    [Fact]
    public void BankFilter_ExcludingEveryRow_LeavesUnderlyingOperationsIntact()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        List<TransferDTO> transfers = [new() { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = BarclaysId, DestinationBankName = "Barclays", Amount = 10m }];
        viewModel.ApplyRefresh(transfers, [], Today.Year, Today.Month, DefaultBanks);

        SelectOnly(viewModel.BankFilter, "Chase");

        viewModel.HasAnyBankOperationsBeforeFilter.Should().BeTrue();
        viewModel.HasBankOperations.Should().BeFalse();
        viewModel.BankOperations.Should().HaveCount(1);
    }
}
