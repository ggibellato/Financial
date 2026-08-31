using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class IncomeSourcesViewModelTests
{
    private static (IncomeSourcesViewModel ViewModel, StubIncomeSourceService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubIncomeSourceService();
        var dialog = new StubDialogService();
        var viewModel = new IncomeSourcesViewModel(service, dialog, new RecordingLogger<IncomeSourcesViewModel>());
        return (viewModel, service, dialog);
    }

    private static IncomeSourceDTO IncomeSource(Guid id, string name, string group = "Salary", bool isActive = true, bool autoSplitToReserve = false, bool hasReferences = false) => new()
    {
        Id = id,
        Name = name,
        Group = group,
        IsActive = isActive,
        AutoSplitToReserve = autoSplitToReserve,
        HasReferences = hasReferences,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesIncomeSourcesFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.IncomeSources = [IncomeSource(Guid.NewGuid(), "Gleison")];

        await viewModel.RefreshAsync();

        viewModel.IncomeSources.Should().ContainSingle(s => s.Name == "Gleison");
    }

    [Fact]
    public async Task CreateIncomeSourceAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowIncomeSourceFormDialogResult = true;
        dialog.OnShowIncomeSourceFormDialog = vm =>
        {
            vm.Name = "Freelance";
            vm.Group = "NonReportable";
            vm.IsActive = true;
            vm.AutoSplitToReserve = true;
        };

        await viewModel.CreateIncomeSourceAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("Freelance");
        service.LastCreateRequest.Group.Should().Be("NonReportable");
        service.LastCreateRequest.AutoSplitToReserve.Should().BeTrue();
        viewModel.IncomeSources.Should().ContainSingle(s => s.Name == "Freelance");
    }

    [Fact]
    public async Task CreateIncomeSourceAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowIncomeSourceFormDialogResult = false;

        await viewModel.CreateIncomeSourceAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateIncomeSourceAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowIncomeSourceFormDialog = vm => vm.Name = "Gleison";
        service.ThrowOnCreate = new InvalidOperationException("An income source named \"Gleison\" already exists.");

        await viewModel.CreateIncomeSourceAsync();

        viewModel.ActionError.Should().Be("An income source named \"Gleison\" already exists.");
    }

    [Fact]
    public async Task EditIncomeSourceAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var incomeSource = IncomeSource(id, "Gleison", group: "Salary", isActive: true, autoSplitToReserve: false);
        dialog.OnShowIncomeSourceFormDialog = vm => vm.AutoSplitToReserve = true;

        await viewModel.EditIncomeSourceAsync(incomeSource);

        dialog.LastIncomeSourceFormDialog!.Name.Should().Be("Gleison");
        dialog.LastIncomeSourceFormDialog.AutoSplitToReserve.Should().BeTrue();
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.AutoSplitToReserve.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteIncomeSourceAsync_HasReferences_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var incomeSource = IncomeSource(Guid.NewGuid(), "Gleison", hasReferences: true);

        await viewModel.DeleteIncomeSourceAsync(incomeSource);

        viewModel.ActionError.Should().Contain("still used by an income entry");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteIncomeSourceAsync_NoReferences_ConfirmsThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var incomeSource = IncomeSource(id, "Gleison");

        await viewModel.DeleteIncomeSourceAsync(incomeSource);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteIncomeSourceAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var incomeSource = IncomeSource(Guid.NewGuid(), "Gleison");

        await viewModel.DeleteIncomeSourceAsync(incomeSource);

        service.LastDeletedId.Should().BeNull();
    }
}
