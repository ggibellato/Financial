using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class CategoryFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithActiveOnAndFlagsOff()
    {
        var viewModel = new CategoryFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Category");
        viewModel.Name.Should().BeEmpty();
        viewModel.Active.Should().BeTrue();
        viewModel.IsInvestment.Should().BeFalse();
        viewModel.IsTithe.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var viewModel = new CategoryFormDialogViewModel("Mercado", currentActive: false, currentIsInvestment: true, currentIsTithe: true);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Category");
        viewModel.Name.Should().Be("Mercado");
        viewModel.Active.Should().BeFalse();
        viewModel.IsInvestment.Should().BeTrue();
        viewModel.IsTithe.Should().BeTrue();
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new CategoryFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new CategoryFormDialogViewModel { Name = "Mercado" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new CategoryFormDialogViewModel { Name = "  Mercado  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("Mercado");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new CategoryFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new CategoryFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
