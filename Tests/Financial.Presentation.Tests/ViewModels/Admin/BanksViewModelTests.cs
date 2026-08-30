using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class BanksViewModelTests
{
    private static (BanksViewModel ViewModel, StubBankService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubBankService();
        var dialog = new StubDialogService();
        var viewModel = new BanksViewModel(service, dialog, new RecordingLogger<BanksViewModel>());
        return (viewModel, service, dialog);
    }

    private static BankDTO Bank(Guid id, string name, bool roundUpEnabled = false, bool hasReferences = false) => new()
    {
        Id = id,
        Name = name,
        RoundUpEnabled = roundUpEnabled,
        OpeningBalance = 0,
        OpeningBalanceDate = default,
        HasReferences = hasReferences,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesBanksFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.Banks = [Bank(Guid.NewGuid(), "Barclays")];

        await viewModel.RefreshAsync();

        viewModel.Banks.Should().ContainSingle(b => b.Name == "Barclays");
    }

    [Fact]
    public async Task CreateBankAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowBankFormDialogResult = true;
        dialog.OnShowBankFormDialog = vm =>
        {
            vm.Name = "New Bank";
            vm.RoundUpEnabled = true;
        };

        await viewModel.CreateBankAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("New Bank");
        service.LastCreateRequest.RoundUpEnabled.Should().BeTrue();
        viewModel.Banks.Should().ContainSingle(b => b.Name == "New Bank");
    }

    [Fact]
    public async Task CreateBankAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowBankFormDialogResult = false;

        await viewModel.CreateBankAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateBankAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowBankFormDialog = vm => vm.Name = "Barclays";
        service.ThrowOnCreate = new InvalidOperationException("A bank named \"Barclays\" already exists.");

        await viewModel.CreateBankAsync();

        viewModel.ActionError.Should().Be("A bank named \"Barclays\" already exists.");
    }

    [Fact]
    public async Task EditBankAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var bank = Bank(id, "Barclays");
        dialog.OnShowBankFormDialog = vm => vm.RoundUpEnabled = true;

        await viewModel.EditBankAsync(bank);

        dialog.LastBankFormDialog!.Name.Should().Be("Barclays");
        dialog.LastBankFormDialog.RoundUpEnabled.Should().BeTrue();
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.RoundUpEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBankAsync_HasReferences_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var bank = Bank(Guid.NewGuid(), "Barclays", hasReferences: true);

        await viewModel.DeleteBankAsync(bank);

        viewModel.ActionError.Should().Contain("still has balance history or transactions");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBankAsync_NoReferences_ConfirmsThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var bank = Bank(id, "Barclays");

        await viewModel.DeleteBankAsync(bank);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteBankAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var bank = Bank(Guid.NewGuid(), "Barclays");

        await viewModel.DeleteBankAsync(bank);

        service.LastDeletedId.Should().BeNull();
    }
}
