using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class IncomeSourceFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithSalaryGroupActiveOnAndAutoSplitOff()
    {
        var viewModel = new IncomeSourceFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Income Source");
        viewModel.Name.Should().BeEmpty();
        viewModel.Group.Should().Be("Salary");
        viewModel.IsActive.Should().BeTrue();
        viewModel.AutoSplitToReserve.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var viewModel = new IncomeSourceFormDialogViewModel("Gleison", currentGroup: "NonReportable", currentIsActive: false, currentAutoSplitToReserve: true);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Income Source");
        viewModel.Name.Should().Be("Gleison");
        viewModel.Group.Should().Be("NonReportable");
        viewModel.IsActive.Should().BeFalse();
        viewModel.AutoSplitToReserve.Should().BeTrue();
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new IncomeSourceFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new IncomeSourceFormDialogViewModel { Name = "Gleison" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new IncomeSourceFormDialogViewModel { Name = "  Gleison  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("Gleison");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new IncomeSourceFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new IncomeSourceFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
