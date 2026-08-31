using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class CreditCardsViewModelTests
{
    private static (CreditCardsViewModel ViewModel, StubCreditCardService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubCreditCardService();
        var dialog = new StubDialogService();
        var viewModel = new CreditCardsViewModel(service, dialog, new RecordingLogger<CreditCardsViewModel>());
        return (viewModel, service, dialog);
    }

    private static CreditCardDTO CreditCard(Guid id, string name, bool isActive = true, DateOnly? nextInvoiceDueDate = null, bool hasReferences = false) => new()
    {
        Id = id,
        Name = name,
        IsActive = isActive,
        NextInvoiceDueDate = nextInvoiceDueDate,
        HasReferences = hasReferences,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesCreditCardsFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.CreditCards = [CreditCard(Guid.NewGuid(), "BaAmex")];

        await viewModel.RefreshAsync();

        viewModel.CreditCards.Should().ContainSingle(c => c.Name == "BaAmex");
    }

    [Fact]
    public async Task CreateCreditCardAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowCreditCardFormDialogResult = true;
        dialog.OnShowCreditCardFormDialog = vm =>
        {
            vm.Name = "Nubank";
            vm.IsActive = true;
        };

        await viewModel.CreateCreditCardAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("Nubank");
        service.LastCreateRequest.IsActive.Should().BeTrue();
        viewModel.CreditCards.Should().ContainSingle(c => c.Name == "Nubank");
    }

    [Fact]
    public async Task CreateCreditCardAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowCreditCardFormDialogResult = false;

        await viewModel.CreateCreditCardAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateCreditCardAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowCreditCardFormDialog = vm => vm.Name = "BaAmex";
        service.ThrowOnCreate = new InvalidOperationException("A credit card named \"BaAmex\" already exists.");

        await viewModel.CreateCreditCardAsync();

        viewModel.ActionError.Should().Be("A credit card named \"BaAmex\" already exists.");
    }

    [Fact]
    public async Task EditCreditCardAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 9, 5);
        var creditCard = CreditCard(id, "BaAmex", isActive: true, nextInvoiceDueDate: dueDate);
        dialog.OnShowCreditCardFormDialog = vm => vm.IsActive = false;

        await viewModel.EditCreditCardAsync(creditCard);

        dialog.LastCreditCardFormDialog!.Name.Should().Be("BaAmex");
        dialog.LastCreditCardFormDialog.NextInvoiceDueDate.Should().Be(dueDate.ToDateTime(TimeOnly.MinValue));
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.Name.Should().Be("BaAmex");
        service.LastUpdateRequest.Value.Request.IsActive.Should().BeFalse();
        service.LastUpdateRequest.Value.Request.NextInvoiceDueDate.Should().Be(dueDate);
    }

    [Fact]
    public async Task DeleteCreditCardAsync_HasReferences_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var creditCard = CreditCard(Guid.NewGuid(), "BaAmex", hasReferences: true);

        await viewModel.DeleteCreditCardAsync(creditCard);

        viewModel.ActionError.Should().Contain("still referenced by a statement or expense");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCreditCardAsync_NoReferences_ConfirmsThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var creditCard = CreditCard(id, "BaAmex");

        await viewModel.DeleteCreditCardAsync(creditCard);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteCreditCardAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var creditCard = CreditCard(Guid.NewGuid(), "BaAmex");

        await viewModel.DeleteCreditCardAsync(creditCard);

        service.LastDeletedId.Should().BeNull();
    }
}
