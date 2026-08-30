using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class BrokerFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithDefaultCurrency()
    {
        var viewModel = new BrokerFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Broker");
        viewModel.Name.Should().BeEmpty();
        viewModel.Currency.Should().Be(BrokerFormDialogViewModel.Currencies[0]);
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var viewModel = new BrokerFormDialogViewModel("XPI", "USD");

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Broker");
        viewModel.Name.Should().Be("XPI");
        viewModel.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new BrokerFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new BrokerFormDialogViewModel { Name = "XPI" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new BrokerFormDialogViewModel { Name = "  XPI  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("XPI");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new BrokerFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new BrokerFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
