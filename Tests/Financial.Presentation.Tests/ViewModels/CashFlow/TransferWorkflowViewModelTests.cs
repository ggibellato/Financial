using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class TransferWorkflowViewModelTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static (TransferWorkflowViewModel ViewModel, StubTransferService Service, ObservableCollection<BankDTO> Banks) CreateViewModel(Func<Task>? refresh = null)
    {
        var transferService = new StubTransferService();
        var banks = new ObservableCollection<BankDTO>
        {
            new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
            new() { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
        };
        var viewModel = new TransferWorkflowViewModel(transferService, banks, refresh ?? (() => Task.CompletedTask));
        return (viewModel, transferService, banks);
    }

    [Fact]
    public async Task AddTransfer_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, transfers, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks[0].Id);
        viewModel.TransferFormDate = DateTime.Today;
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
        viewModel.TransferFormDate = DateTime.Today;
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
        viewModel.TransferFormDate = DateTime.Today;
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
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m };

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
        viewModel.TransferFormDate = DateTime.Today;
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
        viewModel.TransferFormDate = DateTime.Today;
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
        viewModel.TransferFormDate = DateTime.Today;
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
        viewModel.TransferFormDate = DateTime.Today;
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

        viewModel.TransferFormDate = DateTime.Today;
        await viewModel.SaveTransferAsync();

        viewModel.DateFieldError.Should().BeNull();
    }

    [Fact]
    public async Task ShowCreateTransferForm_AfterSuccessfulCreate_PersistsDateSourceAndDestinationBank()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(null);
        var usedDate = DateTime.Today.AddDays(-2);
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
        viewModel.TransferFormDate = DateTime.Today;
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
        viewModel.TransferFormDate = DateTime.Today;
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
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 33m };

        viewModel.EditTransferCommand.Execute(transfer);

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.IsEditingTransfer.Should().BeTrue();
        viewModel.TransferFormAmount.Should().Be("33");
    }
}
