using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class InvestmentAccountsViewModelTests
{
    private static (InvestmentAccountsViewModel ViewModel, StubInvestmentAccountService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubInvestmentAccountService();
        var dialog = new StubDialogService();
        var viewModel = new InvestmentAccountsViewModel(service, dialog, new RecordingLogger<InvestmentAccountsViewModel>());
        return (viewModel, service, dialog);
    }

    private static InvestmentAccountDTO Account(Guid id, string name, bool isActive = true, bool isLiability = false, bool hasNonZeroInvestmentSnapshot = false) => new()
    {
        Id = id,
        Name = name,
        IsActive = isActive,
        IsLiability = isLiability,
        HasNonZeroInvestmentSnapshot = hasNonZeroInvestmentSnapshot,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesInvestmentAccountsFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.InvestmentAccounts = [Account(Guid.NewGuid(), "ChaseSave")];

        await viewModel.RefreshAsync();

        viewModel.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChaseSave");
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
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
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowInvestmentAccountFormDialogResult = false;

        await viewModel.CreateInvestmentAccountAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowInvestmentAccountFormDialog = vm => vm.Name = "ChaseSave";
        service.ThrowOnCreate = new InvalidOperationException("An investment account named \"ChaseSave\" already exists.");

        await viewModel.CreateInvestmentAccountAsync();

        viewModel.ActionError.Should().Be("An investment account named \"ChaseSave\" already exists.");
    }

    [Fact]
    public async Task EditInvestmentAccountAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var account = Account(id, "ChaseSave");
        dialog.OnShowInvestmentAccountFormDialog = vm => vm.IsLiability = true;

        await viewModel.EditInvestmentAccountAsync(account);

        dialog.LastInvestmentAccountFormDialog!.Name.Should().Be("ChaseSave");
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.IsLiability.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_HasNonZeroInvestmentSnapshot_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var account = Account(Guid.NewGuid(), "ChaseSave", hasNonZeroInvestmentSnapshot: true);

        await viewModel.DeleteInvestmentAccountAsync(account);

        viewModel.ActionError.Should().Contain("non-zero balance");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_NoNonZeroInvestmentSnapshot_ConfirmsThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var account = Account(id, "ChaseSave", hasNonZeroInvestmentSnapshot: false);

        await viewModel.DeleteInvestmentAccountAsync(account);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var account = Account(Guid.NewGuid(), "ChaseSave");

        await viewModel.DeleteInvestmentAccountAsync(account);

        service.LastDeletedId.Should().BeNull();
    }
}
