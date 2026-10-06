using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

[Trait("Category", "Unit")]
public class TransferWorkflowViewModelTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static (TransferWorkflowViewModel ViewModel, StubTransferService Service, ObservableCollection<BankDTO> Banks) CreateViewModel(Func<Task>? refresh = null)
    {
        var transferService = new StubTransferService();
        var banks = new ObservableCollection<BankDTO>
        {
            new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = TestClock.Today, HasReferences = false },
            new() { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = TestClock.Today, HasReferences = false },
        };
        var viewModel = new TransferWorkflowViewModel(transferService, banks, TestClock.At(), refresh ?? (() => Task.CompletedTask));
        return (viewModel, transferService, banks);
    }

    [Fact]
    public async Task AddTransfer_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().NotBeNull();
        transfers.LastCreateRequest!.SourceBankId.Should().Be(BarclaysId);
        transfers.LastCreateRequest.DestinationBankId.Should().Be(ChaseId);
        transfers.LastCreateRequest.Amount.Should().Be(75m);
        viewModel.IsTransferFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddTransfer_SameSourceAndDestination_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[0].Id;
        viewModel.TransferFormAmount = "75";

        viewModel.SaveTransferCommand.CanExecute(null).Should().BeFalse();
        viewModel.SameBankTransferError.Should().NotBeNullOrEmpty();

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.TransferSaveError.Should().NotBeNullOrEmpty();

        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.SaveTransferCommand.CanExecute(null).Should().BeTrue();
        viewModel.SameBankTransferError.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTransfer_BackendRejects_KeepsFormOpenWithValuesAndShowsServerError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        transfers.ThrowOnAdd = "Insufficient funds in source bank.";
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.TransferSaveError.Should().Be("Insufficient funds in source bank.");
        viewModel.TransferFormAmount.Should().Be("75");
        viewModel.TransferFormDestinationBank.Should().Be(banks[1].Id);
    }

    [Fact]
    public async Task EditTransfer_ValidForm_CallsUpdateServiceWithCorrectId()
    {
        var (viewModel, transfers, _) = CreateViewModel();
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = TestClock.Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m };

        viewModel.EditTransferCommand.Execute(transfer);
        viewModel.TransferFormAmount = "60";

        await viewModel.SaveTransferAsync();

        transfers.LastUpdateRequest.Should().NotBeNull();
        transfers.LastUpdateRequest!.Value.Id.Should().Be(transfer.Id);
        transfers.LastUpdateRequest.Value.Request.Amount.Should().Be(60m);
    }

    [Fact]
    public void MoveMoneyCommand_GenericEntryPoint_OpensFormWithNoRowContext()
    {
        var (viewModel, _, banks) = CreateViewModel();

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.TransferFormSourceBank.Should().Be(banks[0].Id);
    }

    [Fact]
    public async Task DateFieldError_MissingDate_MatchesSaveError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = null;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.DateFieldError.Should().Be(viewModel.TransferSaveError);
        viewModel.AmountFieldError.Should().BeNull();
    }

    [Fact]
    public async Task SourceBankFieldError_MissingSourceBank_MatchesSaveError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(null);
        viewModel.TransferFormSourceBank = null;
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.SourceBankFieldError.Should().Be(viewModel.TransferSaveError);
    }

    [Fact]
    public async Task DestinationBankFieldError_MissingDestinationBank_MatchesSaveError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = null;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.DestinationBankFieldError.Should().Be(viewModel.TransferSaveError);
    }

    [Fact]
    public async Task DestinationBankFieldError_SameSourceAndDestination_MatchesSaveError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[0].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.DestinationBankFieldError.Should().Be(viewModel.TransferSaveError);
    }

    [Fact]
    public async Task AmountFieldError_NonPositive_MatchesSaveError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "0";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.AmountFieldError.Should().Be(viewModel.TransferSaveError);
    }

    [Fact]
    public async Task FieldErrors_ClearAfterSuccessfulSave()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = null;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";
        await viewModel.SaveTransferAsync();
        viewModel.DateFieldError.Should().NotBeNull();

        viewModel.TransferFormDate = TestClock.LocalToday;
        await viewModel.SaveTransferAsync();

        viewModel.DateFieldError.Should().BeNull();
    }

    [Fact]
    public async Task ShowCreateTransferForm_AfterSuccessfulCreate_PersistsDateSourceAndDestinationBank()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(null);
        var usedDate = TestClock.LocalToday.AddDays(-2);
        viewModel.TransferFormDate = usedDate;
        viewModel.TransferFormSourceBank = banks[1].Id;
        viewModel.TransferFormDestinationBank = banks[0].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.TransferFormDate.Should().Be(usedDate);
        viewModel.TransferFormSourceBank.Should().Be(banks[1].Id);
        viewModel.TransferFormDestinationBank.Should().Be(banks[0].Id);
    }

    [Fact]
    public async Task ShowCreateTransferForm_ExplicitSourceBankOverridesPersistedSourceBank()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(null);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormSourceBank = banks[1].Id;
        viewModel.TransferFormDestinationBank = banks[0].Id;
        viewModel.TransferFormAmount = "75";
        await viewModel.SaveTransferAsync();

        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);

        viewModel.TransferFormSourceBank.Should().Be(banks[0].Id);
    }

    [Fact]
    public async Task ShowCreateTransferForm_AfterSuccessfulCreate_AmountAndNoteStayBlank()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";
        viewModel.TransferFormNote = "Round-up top-up";

        await viewModel.SaveTransferAsync();

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.TransferFormAmount.Should().BeEmpty();
        viewModel.TransferFormNote.Should().BeEmpty();
    }

    [Fact]
    public void EditBankOperation_Transfer_OpensTransferFormPrefilled()
    {
        var (viewModel, _, _) = CreateViewModel();
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = TestClock.Today, SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m };

        viewModel.EditTransferCommand.Execute(transfer);

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.IsEditingTransfer.Should().BeTrue();
        viewModel.TransferFormAmount.Should().Be("33");
    }

    private static async Task SaveTransferBetween(TransferWorkflowViewModel viewModel, Guid source, Guid destination)
    {
        viewModel.ShowMoveMoneyFormCommand.Execute(source);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = destination;
        viewModel.TransferFormAmount = "75";
        await viewModel.SaveTransferAsync();
    }

    [Fact]
    public void IsSameBankTransfer_IsTrueOnlyWhenBothBanksAreSetAndEqual()
    {
        var (viewModel, _, _) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(BarclaysId);

        viewModel.IsSameBankTransfer.Should().BeFalse("only the source is set");
        viewModel.SameBankTransferError.Should().BeEmpty();

        viewModel.TransferFormDestinationBank = BarclaysId;
        viewModel.IsSameBankTransfer.Should().BeTrue();
        viewModel.SameBankTransferError.Should().Be("Source and destination must be different banks.");
        viewModel.SaveTransferCommand.CanExecute(null).Should().BeFalse();

        viewModel.TransferFormDestinationBank = ChaseId;
        viewModel.IsSameBankTransfer.Should().BeFalse();
        viewModel.SaveTransferCommand.CanExecute(null).Should().BeTrue();

        viewModel.TransferFormSourceBank = null;
        viewModel.IsSameBankTransfer.Should().BeFalse("only the destination is set");
    }

    [Fact]
    public async Task TransferGeneralSaveError_BackendRejects_ShowsTheMessageAsAGeneralError()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        transfers.ThrowOnAdd = "Storage unavailable";
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = TestClock.LocalToday;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        viewModel.TransferGeneralSaveError.Should().Be("Storage unavailable");
    }

    [Fact]
    public async Task TransferGeneralSaveError_FieldValidationFails_IsNotShownAsAGeneralError()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = null;
        viewModel.TransferFormDestinationBank = banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        viewModel.DateFieldError.Should().NotBeNull();
        viewModel.TransferGeneralSaveError.Should().BeNull();
    }

    [Fact]
    public async Task ShowCreateTransferForm_LastUsedSourceBankWasRemoved_FallsBackToTheFirstBankAndClearsTheDestination()
    {
        var (viewModel, _, banks) = CreateViewModel();
        await SaveTransferBetween(viewModel, ChaseId, BarclaysId);
        banks.Remove(banks.Single(b => b.Id == ChaseId));

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.TransferFormSourceBank.Should().Be(BarclaysId);
        viewModel.TransferFormDestinationBank.Should().BeNull("the last destination now equals the source");
    }

    [Fact]
    public async Task ShowCreateTransferForm_LastUsedDestinationBankWasRemoved_LeavesTheDestinationEmpty()
    {
        var (viewModel, _, banks) = CreateViewModel();
        await SaveTransferBetween(viewModel, BarclaysId, ChaseId);
        banks.Remove(banks.Single(b => b.Id == ChaseId));

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.TransferFormSourceBank.Should().Be(BarclaysId);
        viewModel.TransferFormDestinationBank.Should().BeNull();
    }

    [Fact]
    public async Task ShowCreateTransferForm_NoBanksAvailable_LeavesBothBanksEmpty()
    {
        var (viewModel, _, banks) = CreateViewModel();
        await SaveTransferBetween(viewModel, BarclaysId, ChaseId);
        banks.Clear();

        viewModel.ShowMoveMoneyFormCommand.Execute(null);

        viewModel.TransferFormSourceBank.Should().BeNull();
        viewModel.TransferFormDestinationBank.Should().BeNull();
    }

    [Fact]
    public void EditTransferCommand_NoTransferSupplied_LeavesTheFormClosed()
    {
        var (viewModel, _, _) = CreateViewModel();

        viewModel.EditTransferCommand.Execute(null);

        viewModel.IsTransferFormOpen.Should().BeFalse();
        viewModel.IsEditingTransfer.Should().BeFalse();
    }
}