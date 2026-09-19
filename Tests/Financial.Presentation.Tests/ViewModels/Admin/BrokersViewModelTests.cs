using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;

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
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0, CostBasisMethod = CostBasisMethod.AverageCost };
        dialog.OnShowBrokerFormDialog = vm => vm.Currency = "USD";

        await viewModel.EditBrokerAsync(broker);

        dialog.LastBrokerFormDialog!.Name.Should().Be("XPI");
        dialog.LastBrokerFormDialog.Currency.Should().Be("USD");
        dialog.LastBrokerFormDialog.CostBasisMethod.Should().Be("AverageCost");
        service.LastUpdateRequest!.Value.CurrentName.Should().Be("XPI");
        service.LastUpdateRequest.Value.Request.Currency.Should().Be("USD");
        service.LastUpdateRequest.Value.Scope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public async Task EditBrokerAsync_HistoricBroker_ForwardsHistoricScopeToBothUpdateCalls()
    {
        // Regression: without forwarding the broker's own status as the scope, resolving a broker
        // for a Historic record that also has an Active one under the same name would pick the
        // wrong (Active) record and either fail with "not found" or edit the wrong broker.
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Historic", PortfolioCount = 0, CostBasisMethod = CostBasisMethod.AverageCost };
        dialog.OnShowBrokerFormDialog = vm => vm.CostBasisMethod = "FIFO";

        await viewModel.EditBrokerAsync(broker);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            service.LastUpdateRequest!.Value.Scope.Should().Be(InvestmentScope.Historic);
            service.LastSetCostBasisMethodRequest!.Value.Scope.Should().Be(InvestmentScope.Historic);
        }
    }

    [Fact]
    public async Task CreateBrokerAsync_PassesSelectedCostBasisMethod()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowBrokerFormDialog = vm =>
        {
            vm.Name = "New Broker";
            vm.CostBasisMethod = "FIFO";
        };

        await viewModel.CreateBrokerAsync();

        service.LastCreateRequest!.CostBasisMethod.Should().Be(CostBasisMethod.FIFO);
    }

    [Fact]
    public async Task EditBrokerAsync_CostBasisMethodUnchanged_DoesNotCallSetCostBasisMethod()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0, CostBasisMethod = CostBasisMethod.AverageCost };

        await viewModel.EditBrokerAsync(broker);

        service.LastSetCostBasisMethodRequest.Should().BeNull();
    }

    [Fact]
    public async Task EditBrokerAsync_CostBasisMethodChanged_CallsSetCostBasisMethodAfterUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0, CostBasisMethod = CostBasisMethod.AverageCost };
        dialog.OnShowBrokerFormDialog = vm => vm.CostBasisMethod = "FIFO";

        await viewModel.EditBrokerAsync(broker);

        service.LastSetCostBasisMethodRequest.Should().Be(("XPI", CostBasisMethod.FIFO, InvestmentScope.Active));
        viewModel.ActionError.Should().BeNull();
    }

    [Fact]
    public async Task EditBrokerAsync_SetCostBasisMethodFails_SurfacesSavedButErrorWithoutLosingRename()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0, CostBasisMethod = CostBasisMethod.AverageCost };
        dialog.OnShowBrokerFormDialog = vm => vm.CostBasisMethod = "FIFO";
        service.ThrowOnSetCostBasisMethod = new InvalidOperationException("regeneration failed");

        await viewModel.EditBrokerAsync(broker);

        service.LastUpdateRequest.Should().NotBeNull();
        viewModel.ActionError.Should().Contain("XPI").And.Contain("regeneration failed");
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

    [Fact]
    public async Task RefreshAsync_ServiceThrows_SetsErrorAndLogsFailure()
    {
        var service = new StubBrokerService { ThrowOnGetBrokers = new InvalidOperationException("boom") };
        var logger = new RecordingLogger<BrokersViewModel>();
        var viewModel = new BrokersViewModel(service, new StubDialogService(), logger);

        await viewModel.RefreshAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.Error.Should().Be("boom");
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains(nameof(InvalidOperationException)));
    }

    [Fact]
    public async Task EditBrokerAsync_NullBroker_ReturnsWithoutShowingDialog()
    {
        var (viewModel, _, dialog) = CreateViewModel();

        await viewModel.EditBrokerAsync(null);

        dialog.LastBrokerFormDialog.Should().BeNull();
    }

    [Fact]
    public async Task EditBrokerAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowBrokerFormDialogResult = false;
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 };

        await viewModel.EditBrokerAsync(broker);

        service.LastUpdateRequest.Should().BeNull();
    }

    [Fact]
    public async Task EditBrokerAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.ThrowOnUpdate = new InvalidOperationException("A broker named \"XPI\" already exists.");
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 };

        await viewModel.EditBrokerAsync(broker);

        viewModel.ActionError.Should().Be("A broker named \"XPI\" already exists.");
    }

    [Fact]
    public async Task DeleteBrokerAsync_NullBroker_ReturnsWithoutConfirming()
    {
        var (viewModel, _, dialog) = CreateViewModel();

        await viewModel.DeleteBrokerAsync(null);

        dialog.LastConfirmMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBrokerAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.ThrowOnDelete = new InvalidOperationException("Broker is referenced elsewhere.");
        var broker = new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 };

        await viewModel.DeleteBrokerAsync(broker);

        viewModel.ActionError.Should().Be("Broker is referenced elsewhere.");
    }

    [Fact]
    public async Task Properties_ExposeExpectedDefaultsAndCommands()
    {
        var (viewModel, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.IsLoading.Should().BeFalse();
        viewModel.RetryCommand.Should().NotBeNull();
        viewModel.CreateBrokerCommand.Should().NotBeNull();
        viewModel.EditBrokerCommand.Should().NotBeNull();
        viewModel.DeleteBrokerCommand.Should().NotBeNull();
    }
}
