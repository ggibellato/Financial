using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class PortfolioFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithFirstActiveBrokerSelected()
    {
        var viewModel = new PortfolioFormDialogViewModel(["XPI", "Avenue"]);

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Portfolio");
        viewModel.Name.Should().BeEmpty();
        viewModel.BrokerName.Should().Be("XPI");
        viewModel.CanChangeBroker.Should().BeTrue();
        viewModel.ActiveBrokerNames.Should().Equal("XPI", "Avenue");
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilledWithFixedBroker()
    {
        var viewModel = new PortfolioFormDialogViewModel([], "XPI", "Default");

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Portfolio");
        viewModel.Name.Should().Be("Default");
        viewModel.BrokerName.Should().Be("XPI");
        viewModel.CanChangeBroker.Should().BeFalse();
        viewModel.ActiveBrokerNames.Should().Equal("XPI");
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new PortfolioFormDialogViewModel(["XPI"]);

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Constructor_NoActiveBrokersOnCreate_StartsInvalidWithBrokerMessage()
    {
        var viewModel = new PortfolioFormDialogViewModel([]) { Name = "Default" };

        viewModel.ValidationMessage.Should().Be("A broker is required.");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new PortfolioFormDialogViewModel(["XPI"]) { Name = "Default" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new PortfolioFormDialogViewModel(["XPI"]) { Name = "  Default  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("Default");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new PortfolioFormDialogViewModel(["XPI"]);
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new PortfolioFormDialogViewModel(["XPI"]);
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
