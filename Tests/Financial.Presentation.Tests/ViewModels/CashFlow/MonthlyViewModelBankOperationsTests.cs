using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

/// <summary>
/// Covers the Bank tab's flat, cross-bank operations list, its bank filter, and the
/// bank-picker-first Balance Adjustment flow introduced by P22-F02.
/// </summary>
public class MonthlyViewModelBankOperationsTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static (
        MonthlyViewModel ViewModel,
        StubBankService Banks,
        StubTransferService Transfers,
        StubBalanceAdjustmentService Adjustments) CreateViewModel(bool confirmDeletes = true)
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService
        {
            Banks =
            [
                new BankDTO { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
                new BankDTO { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
            ],
        };
        var incomeSources = new StubIncomeSourceService();
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cards = new StubCardStatementService();
        var creditCards = new StubCreditCardService();
        var categories = new StubCategoryService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cards, creditCards, categories, confirm: _ => confirmDeletes, new RecordingTelemetryTracer());
        return (viewModel, banks, transfers, adjustments);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task BuildBankOperations_CombinesTransfersAndAdjustments_SortedNewestFirst()
    {
        var (viewModel, _, transfers, adjustments) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = Today.AddDays(-1), SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m }];
        adjustments.AdjustmentsByBank[BarclaysId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }];

        await viewModel.RefreshAsync();

        viewModel.BankOperations.Should().HaveCount(2);
        viewModel.BankOperations[0].Kind.Should().Be(BankOperationKind.Adjustment);
        viewModel.BankOperations[1].Kind.Should().Be(BankOperationKind.Transfer);
    }

    [Fact]
    public async Task BuildBankOperations_TransferRow_ShowsSourceArrowDestinationLabel()
    {
        var (viewModel, _, transfers, _) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m }];

        await viewModel.RefreshAsync();

        var row = viewModel.BankOperations.Single();
        row.BankLabel.Should().Be("Barclays → Chase");
        row.Kind.Should().Be(BankOperationKind.Transfer);
        row.DisplayAmount.Should().Be(50m);
    }

    [Fact]
    public async Task BuildBankOperations_AdjustmentRow_ShowsSingleBankLabelAndSignedDelta()
    {
        var (viewModel, _, _, adjustments) = CreateViewModel();
        adjustments.AdjustmentsByBank[BarclaysId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = -12.5m }];

        await viewModel.RefreshAsync();

        var row = viewModel.BankOperations.Single();
        row.BankLabel.Should().Be("Barclays");
        row.Kind.Should().Be(BankOperationKind.Adjustment);
        row.DisplayAmount.Should().Be(-12.5m);
    }

    [Fact]
    public async Task BuildBankOperations_AdjustmentOutsideSelectedMonth_Excluded()
    {
        var (viewModel, _, _, adjustments) = CreateViewModel();
        adjustments.AdjustmentsByBank[BarclaysId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today.AddMonths(-2), BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }];

        await viewModel.RefreshAsync();

        viewModel.BankOperations.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildBankOperations_DuplicateBankNames_DoesNotThrow()
    {
        var (viewModel, banks, _, adjustments) = CreateViewModel();
        var duplicateId = Guid.NewGuid();
        banks.Banks.Add(new BankDTO { Id = duplicateId, Name = "Barclays", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = Today });
        adjustments.AdjustmentsByBank[BarclaysId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }];
        adjustments.AdjustmentsByBank[duplicateId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = duplicateId, BankName = "Barclays", TargetBalance = 50m, Delta = -1m }];

        await viewModel.RefreshAsync();

        viewModel.Error.Should().BeNull();
        viewModel.BankOperations.Should().HaveCount(2);
    }

    [Fact]
    public async Task BankFilter_DefaultsToAllBanks_ShowsEveryRow()
    {
        var (viewModel, _, transfers, adjustments) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m }];
        adjustments.AdjustmentsByBank[ChaseId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = ChaseId, BankName = "Chase", TargetBalance = 100m, Delta = 5m }];

        await viewModel.RefreshAsync();

        viewModel.SelectedBankFilter.Should().Be(MonthlyViewModel.AllBanksFilter);
        viewModel.FilteredBankOperations.Count.Should().Be(viewModel.BankOperations.Count);
    }

    [Fact]
    public async Task BankFilter_SelectingBank_MatchesTransferAsSourceOrDestination()
    {
        var (viewModel, _, transfers, _) = CreateViewModel();
        transfers.Transfers =
        [
            new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 10m },
            new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = ChaseId, SourceBankName = "Chase", DestinationBankId = BarclaysId, DestinationBankName = "Barclays", Amount = 20m },
        ];
        await viewModel.RefreshAsync();

        viewModel.SelectedBankFilter = "Barclays";

        viewModel.FilteredBankOperations.Should().HaveCount(2);
        viewModel.FilteredBankOperations.Should().OnlyContain(r => r.SourceBank == "Barclays" || r.DestinationBank == "Barclays");
    }

    [Fact]
    public async Task BankFilter_SelectingBank_MatchesAdjustmentExactBankOnly()
    {
        var (viewModel, _, _, adjustments) = CreateViewModel();
        adjustments.AdjustmentsByBank[BarclaysId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m }];
        adjustments.AdjustmentsByBank[ChaseId] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = ChaseId, BankName = "Chase", TargetBalance = 50m, Delta = -1m }];
        await viewModel.RefreshAsync();

        viewModel.SelectedBankFilter = "Barclays";

        var row = viewModel.FilteredBankOperations.Single();
        row.Bank.Should().Be("Barclays");
    }

    [Fact]
    public async Task BankFilter_SelectingAllBanks_RestoresFullList()
    {
        var (viewModel, _, transfers, _) = CreateViewModel();
        transfers.Transfers =
        [
            new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 10m },
        ];
        await viewModel.RefreshAsync();

        viewModel.SelectedBankFilter = "Chase";
        viewModel.FilteredBankOperations.Should().HaveCount(1);

        viewModel.SelectedBankFilter = MonthlyViewModel.AllBanksFilter;

        viewModel.FilteredBankOperations.Count.Should().Be(viewModel.BankOperations.Count);
    }

    [Fact]
    public async Task BankFilter_ChangingSelection_DoesNotRefetchData()
    {
        var (viewModel, _, transfers, adjustments) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 10m }];
        await viewModel.RefreshAsync();

        var transferCallsBefore = transfers.GetTransfersByMonthCallCount;
        var adjustmentCallsBefore = adjustments.GetAdjustmentsByBankCallCount;

        viewModel.SelectedBankFilter = "Chase";
        viewModel.SelectedBankFilter = "Barclays";
        viewModel.SelectedBankFilter = MonthlyViewModel.AllBanksFilter;

        transfers.GetTransfersByMonthCallCount.Should().Be(transferCallsBefore);
        adjustments.GetAdjustmentsByBankCallCount.Should().Be(adjustmentCallsBefore);
    }

    [Fact]
    public async Task MoveMoneyCommand_GenericEntryPoint_OpensFormWithNoRowContext()
    {
        var (viewModel, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.TransferFormSourceBank.Should().Be(banks.Banks[0].Id);
    }

    [Fact]
    public async Task CorrectBalanceCommand_GenericEntryPoint_OpensFormWithNoBankSelected()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.ShowCorrectBalanceFormCommand.Execute(null);

        viewModel.AdjustmentFormBankName.Should().BeNull();
        viewModel.IsAdjustmentBankSelected.Should().BeFalse();
        viewModel.SaveAdjustmentCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task CorrectBalanceForm_SelectingBank_RevealsFieldsAndCurrentBalance()
    {
        var (viewModel, banks, _, _) = CreateViewModel();
        banks.BankBalances = [new BankBalanceDTO { Bank = "Barclays", Balance = 88m }];
        await viewModel.RefreshAsync();
        viewModel.ShowCorrectBalanceFormCommand.Execute(null);

        viewModel.AdjustmentFormBankName = BarclaysId;

        viewModel.IsAdjustmentBankSelected.Should().BeTrue();
        viewModel.AdjustmentFormCurrentBalance.Should().Be(88m);
        viewModel.AdjustmentFormDate = DateTime.Today;
        viewModel.AdjustmentFormTargetBalance = "90";
        viewModel.SaveAdjustmentCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task CorrectBalanceForm_EditingExistingAdjustment_LocksBankSelection()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };

        viewModel.EditAdjustmentCommand.Execute(adjustment);

        viewModel.AdjustmentFormBankName.Should().Be(BarclaysId);
        viewModel.IsEditingAdjustment.Should().BeTrue();
    }

    [Fact]
    public async Task EditBankOperation_Transfer_OpensTransferFormPrefilled()
    {
        var (viewModel, _, transfers, _) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m }];
        await viewModel.RefreshAsync();
        var row = viewModel.BankOperations.Single();

        viewModel.EditTransferCommand.Execute(row.Transfer);

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.IsEditingTransfer.Should().BeTrue();
        viewModel.TransferFormAmount.Should().Be("33");
    }

    [Fact]
    public async Task DeleteBankOperation_Transfer_ConfirmedCallsTransferDelete()
    {
        var (viewModel, _, transfers, _) = CreateViewModel();
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m };
        transfers.Transfers = [transfer];
        await viewModel.RefreshAsync();
        var row = viewModel.BankOperations.Single();

        await viewModel.DeleteBankOperationAsync(row);

        transfers.LastDeletedId.Should().Be(transfer.Id);
    }

    [Fact]
    public async Task DeleteBankOperation_Adjustment_ConfirmedCallsAdjustmentDelete()
    {
        var (viewModel, _, _, adjustments) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = Today, BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };
        adjustments.AdjustmentsByBank[BarclaysId] = [adjustment];
        await viewModel.RefreshAsync();
        var row = viewModel.BankOperations.Single();

        await viewModel.DeleteBankOperationAsync(row);

        adjustments.LastDeleted.Should().Be((adjustment.BankId, adjustment.Id));
    }

    [Fact]
    public async Task DeleteBankOperation_Declined_SkipsService()
    {
        var (viewModel, _, transfers, adjustments) = CreateViewModel(confirmDeletes: false);
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m };
        transfers.Transfers = [transfer];
        await viewModel.RefreshAsync();
        var row = viewModel.BankOperations.Single();

        await viewModel.DeleteBankOperationAsync(row);

        transfers.LastDeletedId.Should().BeNull();
        adjustments.LastDeleted.Should().BeNull();
    }

    [Fact]
    public async Task BankOperationsEmptyMessage_Unfiltered_ShowsGenericMessage()
    {
        var (viewModel, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.SelectedBankFilter.Should().Be(MonthlyViewModel.AllBanksFilter);
        viewModel.HasBankOperations.Should().BeFalse();
        viewModel.BankOperationsEmptyMessage.Should().Be("No transfers or balance corrections this month.");
    }

    [Fact]
    public async Task BankOperationsEmptyMessage_Filtered_IncludesSelectedBankName()
    {
        var (viewModel, _, transfers, _) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = BarclaysId, DestinationBankName = "Barclays", Amount = 10m }];
        await viewModel.RefreshAsync();

        viewModel.SelectedBankFilter = "Chase";

        viewModel.HasBankOperations.Should().BeFalse();
        viewModel.BankOperationsEmptyMessage.Should().Contain("Chase");
    }
}
