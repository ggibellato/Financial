using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class BrokersViewModelTests
{
    private static (BrokersViewModel ViewModel, StubBrokerService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubBrokerService();
        var dialog = new StubDialogService();
        var viewModel = new BrokersViewModel(service, dialog, new RecordingLogger<BrokersViewModel>());
        return (viewModel, service, dialog);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesBrokersFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.Brokers = [new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 2 }];

        await viewModel.RefreshAsync();

        viewModel.Brokers.Should().ContainSingle(b => b.Name == "XPI");
    }

    [Fact]
    public async Task CreateBrokerAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowBrokerFormDialogResult = true;
        dialog.OnShowBrokerFormDialog = vm =>
        {
            vm.Name = "New Broker";
            vm.Currency = "USD";
        };

        await viewModel.CreateBrokerAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("New Broker");
        service.LastCreateRequest.Currency.Should().Be("USD");
        viewModel.Brokers.Should().ContainSingle(b => b.Name == "New Broker");
    }

    [Fact]
    public async Task CreateBrokerAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowBrokerFormDialogResult = false;

        await viewModel.CreateBrokerAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateBrokerAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowBrokerFormDialog = vm => vm.Name = "XPI";
        service.ThrowOnCreate = new InvalidOperationException("A broker named \"XPI\" already exists.");

        await viewModel.CreateBrokerAsync();

        viewModel.ActionError.Should().Be("A broker named \"XPI\" already exists.");
    }

    [Fact]
    public async Task EditBrokerAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 };
        dialog.OnShowBrokerFormDialog = vm => vm.Currency = "USD";

        await viewModel.EditBrokerAsync(broker);

        dialog.LastBrokerFormDialog!.Name.Should().Be("XPI");
        dialog.LastBrokerFormDialog.Currency.Should().Be("USD");
        service.LastUpdateRequest!.Value.CurrentName.Should().Be("XPI");
        service.LastUpdateRequest.Value.Request.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task DeleteBrokerAsync_WithPortfolios_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 3 };

        await viewModel.DeleteBrokerAsync(broker);

        viewModel.ActionError.Should().Contain("still has 3 portfolio(s)");
        dialog.LastConfirmMessage.Should().BeNull();
        service.LastDeletedName.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBrokerAsync_ActiveAndEmpty_ConfirmsWithArchiveWordingThenDeletes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 };

        await viewModel.DeleteBrokerAsync(broker);

        dialog.LastConfirmMessage.Should().Contain("move to the Historic list");
        service.LastDeletedName.Should().Be("XPI");
    }

    [Fact]
    public async Task DeleteBrokerAsync_HistoricAndEmpty_ConfirmsWithPermanentRemovalWording()
    {
        var (viewModel, _, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Historic", PortfolioCount = 0 };

        await viewModel.DeleteBrokerAsync(broker);

        dialog.LastConfirmMessage.Should().Contain("permanently removed");
    }

    [Fact]
    public async Task DeleteBrokerAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 };

        await viewModel.DeleteBrokerAsync(broker);

        service.LastDeletedName.Should().BeNull();
    }
}
