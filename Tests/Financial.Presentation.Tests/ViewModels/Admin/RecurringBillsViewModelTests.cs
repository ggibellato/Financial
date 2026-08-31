using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class RecurringBillsViewModelTests
{
    private static (RecurringBillsViewModel ViewModel, StubMensaisService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubMensaisService();
        var dialog = new StubDialogService();
        var viewModel = new RecurringBillsViewModel(service, dialog, new RecordingLogger<RecurringBillsViewModel>());
        return (viewModel, service, dialog);
    }

    private static RecurringBillDTO Bill(Guid id, string description, int dueDay = 10, decimal value = 100m, string area = "Brasil", string status = "Unset") => new()
    {
        Id = id,
        DueDay = dueDay,
        Description = description,
        Value = value,
        Area = area,
        Note = string.Empty,
        NitNumber = null,
        MinimumWageValue = null,
        Status = status,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesRecurringBillsFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.Bills = [Bill(Guid.NewGuid(), "Rent")];

        await viewModel.RefreshAsync();

        viewModel.RecurringBills.Should().ContainSingle(b => b.Description == "Rent");
    }

    [Fact]
    public async Task CreateRecurringBillAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowRecurringBillFormDialogResult = true;
        dialog.OnShowRecurringBillFormDialog = vm =>
        {
            vm.DueDay = "20";
            vm.Description = "Utilities";
            vm.Value = "300";
            vm.Area = "UK";
        };

        await viewModel.CreateRecurringBillAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.DueDay.Should().Be(20);
        service.LastCreateRequest.Description.Should().Be("Utilities");
        service.LastCreateRequest.Value.Should().Be(300m);
        service.LastCreateRequest.Area.Should().Be("UK");
        viewModel.RecurringBills.Should().ContainSingle(b => b.Description == "Utilities");
    }

    [Fact]
    public async Task CreateRecurringBillAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowRecurringBillFormDialogResult = false;

        await viewModel.CreateRecurringBillAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateRecurringBillAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowRecurringBillFormDialog = vm =>
        {
            vm.DueDay = "10";
            vm.Description = "Rent";
            vm.Value = "100";
        };
        service.ThrowOnCreate = new InvalidOperationException("Due day must be between 1 and 31.");

        await viewModel.CreateRecurringBillAsync();

        viewModel.ActionError.Should().Be("Due day must be between 1 and 31.");
    }

    [Fact]
    public async Task EditRecurringBillAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var bill = Bill(id, "Rent", dueDay: 10, value: 100m);
        dialog.OnShowRecurringBillFormDialog = vm => vm.Status = "Paid";

        await viewModel.EditRecurringBillAsync(bill);

        dialog.LastRecurringBillFormDialog!.Description.Should().Be("Rent");
        dialog.LastRecurringBillFormDialog.Status.Should().Be("Paid");
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task DeleteRecurringBillAsync_ConfirmsThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var bill = Bill(id, "Rent");

        await viewModel.DeleteRecurringBillAsync(bill);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
        service.LastDeletedId.Should().Be(id);
    }

    [Fact]
    public async Task DeleteRecurringBillAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var bill = Bill(Guid.NewGuid(), "Rent");

        await viewModel.DeleteRecurringBillAsync(bill);

        service.LastDeletedId.Should().BeNull();
    }
}
