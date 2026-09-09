using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class InvestmentAccountsViewModelTests
{
    private static (InvestmentAccountsViewModel ViewModel, StubInvestmentAccountService Service, StubCreditCardService CreditCardService, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubInvestmentAccountService();
        var creditCardService = new StubCreditCardService();
        var dialog = new StubDialogService();
        var viewModel = new InvestmentAccountsViewModel(service, creditCardService, dialog, new RecordingLogger<InvestmentAccountsViewModel>());
        return (viewModel, service, creditCardService, dialog);
    }

    private static InvestmentAccountDTO Account(
        Guid id,
        string name,
        bool isActive = true,
        bool isLiability = false,
        bool hasNonZeroInvestmentSnapshot = false,
        string source = "None",
        Guid? creditCardId = null) => new()
    {
        Id = id,
        Name = name,
        IsActive = isActive,
        IsLiability = isLiability,
        HasNonZeroInvestmentSnapshot = hasNonZeroInvestmentSnapshot,
        Source = source,
        CreditCardId = creditCardId,
    };

    private static CreditCardDTO CreditCard(Guid id, string name, bool isActive = true) => new()
    {
        Id = id,
        Name = name,
        IsActive = isActive,
        HasReferences = true,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesInvestmentAccountsFromService()
    {
        var (viewModel, service, _, _) = CreateViewModel();
        service.InvestmentAccounts = [Account(Guid.NewGuid(), "ChaseSave")];

        await viewModel.RefreshAsync();

        viewModel.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChaseSave");
    }

    [Fact]
    public async Task RefreshAsync_PopulatesCreditCardsFromService()
    {
        var (viewModel, _, creditCardService, _) = CreateViewModel();
        creditCardService.CreditCards = [CreditCard(Guid.NewGuid(), "Platinum Visa 8003")];

        await viewModel.RefreshAsync();

        viewModel.CreditCards.Should().ContainSingle(c => c.Name == "Platinum Visa 8003");
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        dialog.ShowInvestmentAccountFormDialogResult = true;
        dialog.OnShowInvestmentAccountFormDialog = vm =>
        {
            vm.Name = "Monzo Pot";
            vm.IsLiability = true;
        };

        await viewModel.CreateInvestmentAccountAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("Monzo Pot");
        service.LastCreateRequest.IsLiability.Should().BeTrue();
        viewModel.InvestmentAccounts.Should().ContainSingle(a => a.Name == "Monzo Pot");
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        dialog.ShowInvestmentAccountFormDialogResult = false;

        await viewModel.CreateInvestmentAccountAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        dialog.OnShowInvestmentAccountFormDialog = vm => vm.Name = "ChaseSave";
        service.ThrowOnCreate = new InvalidOperationException("An investment account named \"ChaseSave\" already exists.");

        await viewModel.CreateInvestmentAccountAsync();

        viewModel.ActionError.Should().Be("An investment account named \"ChaseSave\" already exists.");
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_DialogOffersOnlyActiveCards()
    {
        var (viewModel, _, creditCardService, dialog) = CreateViewModel();
        var activeCard = CreditCard(Guid.NewGuid(), "Platinum Visa 8003");
        var inactiveCard = CreditCard(Guid.NewGuid(), "Retired Card", isActive: false);
        creditCardService.CreditCards = [activeCard, inactiveCard];
        await viewModel.RefreshAsync();

        await viewModel.CreateInvestmentAccountAsync();

        dialog.LastInvestmentAccountFormDialog!.CreditCardOptions.Should().ContainSingle(c => c.Id == activeCard.Id);
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_DialogSourceCreditCard_PassesSourceAndCreditCardIdToService()
    {
        var (viewModel, service, creditCardService, dialog) = CreateViewModel();
        var card = CreditCard(Guid.NewGuid(), "Platinum Visa 8003");
        creditCardService.CreditCards = [card];
        await viewModel.RefreshAsync();
        dialog.OnShowInvestmentAccountFormDialog = vm =>
        {
            vm.Name = "PlatinumVisa8003";
            vm.Source = "CreditCard";
            vm.CreditCardId = card.Id;
        };

        await viewModel.CreateInvestmentAccountAsync();

        service.LastCreateRequest!.Source.Should().Be("CreditCard");
        service.LastCreateRequest.CreditCardId.Should().Be(card.Id);
    }

    [Fact]
    public async Task EditInvestmentAccountAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var account = Account(id, "ChaseSave");
        dialog.OnShowInvestmentAccountFormDialog = vm => vm.IsLiability = true;

        await viewModel.EditInvestmentAccountAsync(account);

        dialog.LastInvestmentAccountFormDialog!.Name.Should().Be("ChaseSave");
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.IsLiability.Should().BeTrue();
    }

    [Fact]
    public async Task EditInvestmentAccountAsync_CurrentlyLinkedInactiveCard_IsIncludedInDialogOptions()
    {
        var (viewModel, _, creditCardService, dialog) = CreateViewModel();
        var inactiveCard = CreditCard(Guid.NewGuid(), "Retired Card", isActive: false);
        creditCardService.CreditCards = [inactiveCard];
        await viewModel.RefreshAsync();
        var account = Account(Guid.NewGuid(), "PlatinumVisa8003", source: "CreditCard", creditCardId: inactiveCard.Id);

        await viewModel.EditInvestmentAccountAsync(account);

        dialog.LastInvestmentAccountFormDialog!.CreditCardOptions.Should().Contain(inactiveCard);
        dialog.LastInvestmentAccountFormDialog!.CreditCardId.Should().Be(inactiveCard.Id);
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_HasNonZeroInvestmentSnapshot_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        var account = Account(Guid.NewGuid(), "ChaseSave", hasNonZeroInvestmentSnapshot: true);

        await viewModel.DeleteInvestmentAccountAsync(account);

        viewModel.ActionError.Should().Contain("non-zero balance");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_NoNonZeroInvestmentSnapshot_ConfirmsThenDeletes()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var account = Account(id, "ChaseSave", hasNonZeroInvestmentSnapshot: false);

        await viewModel.DeleteInvestmentAccountAsync(account);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var account = Account(Guid.NewGuid(), "ChaseSave");

        await viewModel.DeleteInvestmentAccountAsync(account);

        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task EditInvestmentAccountAsync_NullAccount_DoesNothing()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();

        await viewModel.EditInvestmentAccountAsync(null);

        service.LastUpdateRequest.Should().BeNull();
        dialog.LastInvestmentAccountFormDialog.Should().BeNull();
    }

    [Fact]
    public async Task EditInvestmentAccountAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        dialog.ShowInvestmentAccountFormDialogResult = false;
        var account = Account(Guid.NewGuid(), "ChaseSave");

        await viewModel.EditInvestmentAccountAsync(account);

        service.LastUpdateRequest.Should().BeNull();
    }

    [Fact]
    public async Task EditInvestmentAccountAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, _, dialog) = CreateViewModel();
        var account = Account(Guid.NewGuid(), "ChaseSave");
        dialog.OnShowInvestmentAccountFormDialog = vm => vm.IsLiability = true;
        service.ThrowOnUpdate = new InvalidOperationException("Update failed.");

        await viewModel.EditInvestmentAccountAsync(account);

        viewModel.ActionError.Should().Be("Update failed.");
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_NullAccount_DoesNothing()
    {
        var (viewModel, service, _, _) = CreateViewModel();

        await viewModel.DeleteInvestmentAccountAsync(null);

        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, _, _) = CreateViewModel();
        var account = Account(Guid.NewGuid(), "ChaseSave", hasNonZeroInvestmentSnapshot: false);
        service.ThrowOnDelete = new InvalidOperationException("Delete failed.");

        await viewModel.DeleteInvestmentAccountAsync(account);

        viewModel.ActionError.Should().Be("Delete failed.");
    }

    [Fact]
    public void Properties_ExposeExpectedDefaultsAndCommands()
    {
        var (viewModel, _, _, _) = CreateViewModel();

        viewModel.HasError.Should().BeFalse();
        viewModel.RetryCommand.Should().NotBeNull();
        viewModel.CreateInvestmentAccountCommand.Should().NotBeNull();
        viewModel.EditInvestmentAccountCommand.Should().NotBeNull();
        viewModel.DeleteInvestmentAccountCommand.Should().NotBeNull();
    }
}
