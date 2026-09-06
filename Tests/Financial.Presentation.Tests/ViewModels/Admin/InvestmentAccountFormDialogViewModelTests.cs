using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class InvestmentAccountFormDialogViewModelTests
{
    private static readonly CreditCardDTO ActiveCard = new()
    {
        Id = Guid.NewGuid(),
        Name = "Platinum Visa 8003",
        IsActive = true,
        HasReferences = true,
    };

    private static readonly CreditCardDTO InactiveCard = new()
    {
        Id = Guid.NewGuid(),
        Name = "Retired Card",
        IsActive = false,
        HasReferences = true,
    };

    [Fact]
    public void Constructor_NoCurrentName_IsCreateModeWithActiveOnLiabilityOffSourceNone()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]);

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Investment Account");
        viewModel.Name.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
        viewModel.IsLiability.Should().BeFalse();
        viewModel.Source.Should().Be("None");
        viewModel.CreditCardId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCurrentName_IsEditModePreFilled()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([], "ChaseSave", currentIsActive: false, currentIsLiability: true);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Investment Account");
        viewModel.Name.Should().Be("ChaseSave");
        viewModel.IsActive.Should().BeFalse();
        viewModel.IsLiability.Should().BeTrue();
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]);

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]) { Name = "ChaseSave" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndRaisesCloseRequestedTrue()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]) { Name = "  ChaseSave  " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("ChaseSave");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]);
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]);
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }

    [Fact]
    public void Source_None_IsNeitherCreditCardNorReserveBucketsMode()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([ActiveCard]) { Name = "Monzo Pot" };

        viewModel.IsCreditCardSourceMode.Should().BeFalse();
        viewModel.IsReserveBucketsSourceMode.Should().BeFalse();
    }

    [Fact]
    public void Source_CreditCardWithNoCardChosen_IsInvalid()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([ActiveCard]) { Name = "Monzo Pot", Source = "CreditCard" };

        viewModel.IsCreditCardSourceMode.Should().BeTrue();
        viewModel.ValidationMessage.Should().Be("Select a credit card.");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Source_CreditCardWithCardChosen_IsValid()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([ActiveCard])
        {
            Name = "Monzo Pot",
            Source = "CreditCard",
            CreditCardId = ActiveCard.Id,
        };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Source_ReserveBucketsSum_IsValidWithNoCard()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel([]) { Name = "Reservas pessoais", Source = "ReserveBucketsSum" };

        viewModel.IsReserveBucketsSourceMode.Should().BeTrue();
        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Constructor_EditingAccountWithInactiveLinkedCard_IncludesItInOptionsAndPreselectsIt()
    {
        var viewModel = new InvestmentAccountFormDialogViewModel(
            [ActiveCard, InactiveCard],
            "PlatinumVisa8003",
            currentIsActive: true,
            currentIsLiability: true,
            currentSource: "CreditCard",
            currentCreditCardId: InactiveCard.Id);

        viewModel.CreditCardOptions.Should().Contain(InactiveCard);
        viewModel.CreditCardId.Should().Be(InactiveCard.Id);
    }
}
