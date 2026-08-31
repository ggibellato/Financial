using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class InvestmentAccountFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithActiveOnLiabilityOffAndNoAliases()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Investment Account");
        viewModel.Name.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
        viewModel.IsLiability.Should().BeFalse();
        viewModel.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel("ChaseSave", currentIsActive: false, currentIsLiability: true, currentAliases: ["Chase Save"]);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Investment Account");
        viewModel.Name.Should().Be("ChaseSave");
        viewModel.IsActive.Should().BeFalse();
        viewModel.IsLiability.Should().BeTrue();
        viewModel.Aliases.Should().ContainSingle("Chase Save");
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel { Name = "ChaseSave" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddAliasCommand_WithNewAlias_AddsItAndClearsInput()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel { NewAlias = "Chase Save" };

        viewModel.AddAliasCommand.Execute(null);

        viewModel.Aliases.Should().ContainSingle("Chase Save");
        viewModel.NewAlias.Should().BeEmpty();
    }

    [Fact]
    public void AddAliasCommand_WithBlankAlias_DoesNothing()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel { NewAlias = "   " };

        viewModel.AddAliasCommand.Execute(null);

        viewModel.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void AddAliasCommand_WithCaseInsensitiveDuplicate_DoesNotAddTwice()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel(currentAliases: ["Chase Save"]) { NewAlias = "chase save" };

        viewModel.AddAliasCommand.Execute(null);

        viewModel.Aliases.Should().ContainSingle();
    }

    [Fact]
    public void RemoveAliasCommand_RemovesTheMatchingAlias()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel(currentAliases: ["Chase Save", "CS"]);

        viewModel.RemoveAliasCommand.Execute("Chase Save");

        viewModel.Aliases.Should().ContainSingle("CS");
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel { Name = "  ChaseSave  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("ChaseSave");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
