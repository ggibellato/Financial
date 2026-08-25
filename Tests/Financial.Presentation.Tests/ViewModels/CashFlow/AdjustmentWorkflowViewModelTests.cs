using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class AdjustmentWorkflowViewModelTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();

    private static (AdjustmentWorkflowViewModel ViewModel, StubBalanceAdjustmentService Service, ObservableCollection<BankTotalRow> BankTotals) CreateViewModel(Func<Task>? refresh = null)
    {
        var adjustmentService = new StubBalanceAdjustmentService();
        var banks = new ObservableCollection<BankDTO> { new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) } };
        var bankTotals = new ObservableCollection<BankTotalRow>();
        var viewModel = new AdjustmentWorkflowViewModel(adjustmentService, banks, bankTotals, refresh ?? (() => Task.CompletedTask));
        return (viewModel, adjustmentService, bankTotals);
    }

    [Fact]
    public async Task AddBalanceAdjustment_ValidForm_CallsServiceAndShowsDelta()
    {
        var (viewModel, adjustments, bankTotals) = CreateViewModel();
        bankTotals.Add(new BankTotalRow { BankId = BarclaysId, Bank = "Barclays", Balance = 42.5m, RoundUpTotal = 0m });

        viewModel.ShowCorrectBalanceFormCommand.Execute(null);
        viewModel.AdjustmentFormBankName = BarclaysId;
        viewModel.AdjustmentFormCurrentBalance.Should().Be(42.5m);
        viewModel.AdjustmentFormDate = DateTime.Today;
        viewModel.AdjustmentFormTargetBalance = "50";

        await viewModel.SaveAdjustmentAsync();

        adjustments.LastCreateRequest.Should().NotBeNull();
        adjustments.LastCreateRequest!.Value.BankId.Should().Be(BarclaysId);
        adjustments.LastCreateRequest.Value.Request.TargetBalance.Should().Be(50m);
        viewModel.AdjustmentSavedDelta.Should().NotBeNull();
    }

    [Fact]
    public async Task EditAdjustment_ValidForm_CallsUpdateServiceWithCorrectBankAndId()
    {
        var (viewModel, adjustments, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };

        viewModel.EditAdjustmentCommand.Execute(adjustment);
        viewModel.AdjustmentFormTargetBalance = "120";

        await viewModel.SaveAdjustmentAsync();

        adjustments.LastUpdateRequest.Should().NotBeNull();
        adjustments.LastUpdateRequest!.Value.BankId.Should().Be(BarclaysId);
        adjustments.LastUpdateRequest.Value.Id.Should().Be(adjustment.Id);
        adjustments.LastUpdateRequest.Value.Request.TargetBalance.Should().Be(120m);
    }

    [Fact]
    public void CorrectBalanceCommand_GenericEntryPoint_OpensFormWithNoBankSelected()
    {
        var (viewModel, _, _) = CreateViewModel();

        viewModel.ShowCorrectBalanceFormCommand.Execute(null);

        viewModel.AdjustmentFormBankName.Should().BeNull();
        viewModel.IsAdjustmentBankSelected.Should().BeFalse();
        viewModel.SaveAdjustmentCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CorrectBalanceForm_SelectingBank_RevealsFieldsAndCurrentBalance()
    {
        var (viewModel, _, bankTotals) = CreateViewModel();
        bankTotals.Add(new BankTotalRow { BankId = BarclaysId, Bank = "Barclays", Balance = 88m, RoundUpTotal = 0m });
        viewModel.ShowCorrectBalanceFormCommand.Execute(null);

        viewModel.AdjustmentFormBankName = BarclaysId;

        viewModel.IsAdjustmentBankSelected.Should().BeTrue();
        viewModel.AdjustmentFormCurrentBalance.Should().Be(88m);
        viewModel.AdjustmentFormDate = DateTime.Today;
        viewModel.AdjustmentFormTargetBalance = "90";
        viewModel.SaveAdjustmentCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CorrectBalanceForm_EditingExistingAdjustment_LocksBankSelection()
    {
        var (viewModel, _, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };

        viewModel.EditAdjustmentCommand.Execute(adjustment);

        viewModel.AdjustmentFormBankName.Should().Be(BarclaysId);
        viewModel.IsEditingAdjustment.Should().BeTrue();
    }
}
