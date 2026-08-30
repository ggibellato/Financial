using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class CategoriesViewModelTests
{
    private static (CategoriesViewModel ViewModel, StubCategoryService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubCategoryService();
        var dialog = new StubDialogService();
        var viewModel = new CategoriesViewModel(service, dialog, new RecordingLogger<CategoriesViewModel>());
        return (viewModel, service, dialog);
    }

    private static CategoryDTO Category(Guid id, string name, bool active = true, bool isInvestment = false, bool isTithe = false, bool hasReferences = false) => new()
    {
        Id = id,
        Name = name,
        Active = active,
        IsInvestment = isInvestment,
        IsTithe = isTithe,
        HasReferences = hasReferences,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesCategoriesFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.Categories = [Category(Guid.NewGuid(), "Mercado")];

        await viewModel.RefreshAsync();

        viewModel.Categories.Should().ContainSingle(c => c.Name == "Mercado");
    }

    [Fact]
    public async Task CreateCategoryAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowCategoryFormDialogResult = true;
        dialog.OnShowCategoryFormDialog = vm =>
        {
            vm.Name = "Lazer";
            vm.Active = true;
            vm.IsInvestment = true;
            vm.IsTithe = false;
        };

        await viewModel.CreateCategoryAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("Lazer");
        service.LastCreateRequest.Active.Should().BeTrue();
        service.LastCreateRequest.IsInvestment.Should().BeTrue();
        service.LastCreateRequest.IsTithe.Should().BeFalse();
        viewModel.Categories.Should().ContainSingle(c => c.Name == "Lazer");
    }

    [Fact]
    public async Task CreateCategoryAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowCategoryFormDialogResult = false;

        await viewModel.CreateCategoryAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateCategoryAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowCategoryFormDialog = vm => vm.Name = "Mercado";
        service.ThrowOnCreate = new InvalidOperationException("A category named \"Mercado\" already exists.");

        await viewModel.CreateCategoryAsync();

        viewModel.ActionError.Should().Be("A category named \"Mercado\" already exists.");
    }

    [Fact]
    public async Task EditCategoryAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var category = Category(id, "Mercado", active: true, isInvestment: false, isTithe: false);
        dialog.OnShowCategoryFormDialog = vm => vm.IsTithe = true;

        await viewModel.EditCategoryAsync(category);

        dialog.LastCategoryFormDialog!.Name.Should().Be("Mercado");
        dialog.LastCategoryFormDialog.IsTithe.Should().BeTrue();
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.IsTithe.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategoryAsync_HasReferences_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var category = Category(Guid.NewGuid(), "Mercado", hasReferences: true);

        await viewModel.DeleteCategoryAsync(category);

        viewModel.ActionError.Should().Contain("still used by a transaction");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategoryAsync_NoReferences_ConfirmsThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var category = Category(id, "Mercado");

        await viewModel.DeleteCategoryAsync(category);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var category = Category(Guid.NewGuid(), "Mercado");

        await viewModel.DeleteCategoryAsync(category);

        service.LastDeletedId.Should().BeNull();
    }
}
