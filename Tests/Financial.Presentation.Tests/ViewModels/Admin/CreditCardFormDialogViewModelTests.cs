using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class CreditCardFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeActiveWithNoDueDate()
    {
        var viewModel = new CreditCardFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Credit Card");
        viewModel.Name.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
        viewModel.NextInvoiceDueDate.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var dueDate = new DateTime(2026, 9, 5);
        var viewModel = new CreditCardFormDialogViewModel("BaAmex", false, dueDate);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Credit Card");
        viewModel.Name.Should().Be("BaAmex");
        viewModel.IsActive.Should().BeFalse();
        viewModel.NextInvoiceDueDate.Should().Be(dueDate);
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new CreditCardFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new CreditCardFormDialogViewModel { Name = "BaAmex" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new CreditCardFormDialogViewModel { Name = "  BaAmex  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("BaAmex");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new CreditCardFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new CreditCardFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
