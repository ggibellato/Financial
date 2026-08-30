using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class PortfoliosViewModelTests
{
    private static (PortfoliosViewModel ViewModel, StubPortfolioService PortfolioService, StubBrokerService BrokerService, StubDialogService Dialog) CreateViewModel()
    {
        var portfolioService = new StubPortfolioService();
        var brokerService = new StubBrokerService();
        var dialog = new StubDialogService();
        var viewModel = new PortfoliosViewModel(portfolioService, brokerService, dialog, new RecordingLogger<PortfoliosViewModel>());
        return (viewModel, portfolioService, brokerService, dialog);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesPortfoliosFromService()
    {
        var (viewModel, portfolioService, _, _) = CreateViewModel();
        portfolioService.Portfolios = [new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 2 }];

        await viewModel.RefreshAsync();

        viewModel.Portfolios.Should().ContainSingle(p => p.Name == "Default");
    }

    [Fact]
    public async Task CreatePortfolioAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, portfolioService, brokerService, dialog) = CreateViewModel();
        brokerService.Brokers = [new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 }];
        dialog.ShowPortfolioFormDialogResult = true;
        dialog.OnShowPortfolioFormDialog = vm => vm.Name = "New Portfolio";

        await viewModel.CreatePortfolioAsync();

        portfolioService.LastCreateRequest.Should().NotBeNull();
        portfolioService.LastCreateRequest!.BrokerName.Should().Be("XPI");
        portfolioService.LastCreateRequest.Name.Should().Be("New Portfolio");
        viewModel.Portfolios.Should().ContainSingle(p => p.Name == "New Portfolio");
    }

    [Fact]
    public async Task CreatePortfolioAsync_OnlyOffersActiveBrokersToTheDialog()
    {
        var (viewModel, _, brokerService, dialog) = CreateViewModel();
        brokerService.Brokers =
        [
            new BrokerDTO { Name = "XPI", Currency = "BRL", Status = "Active", PortfolioCount = 0 },
            new BrokerDTO { Name = "Avenue", Currency = "USD", Status = "Historic", PortfolioCount = 0 },
        ];

        await viewModel.CreatePortfolioAsync();

        dialog.LastPortfolioFormDialog!.ActiveBrokerNames.Should().Equal("XPI");
    }

    [Fact]
    public async Task CreatePortfolioAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, portfolioService, _, dialog) = CreateViewModel();
        dialog.ShowPortfolioFormDialogResult = false;

        await viewModel.CreatePortfolioAsync();

        portfolioService.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreatePortfolioAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, portfolioService, _, dialog) = CreateViewModel();
        dialog.OnShowPortfolioFormDialog = vm => vm.Name = "Default";
        portfolioService.ThrowOnCreate = new InvalidOperationException("Broker \"XPI\" already has a portfolio named \"Default\".");

        await viewModel.CreatePortfolioAsync();

        viewModel.ActionError.Should().Be("Broker \"XPI\" already has a portfolio named \"Default\".");
    }

    [Fact]
    public async Task EditPortfolioAsync_PreFillsDialogWithFixedBrokerAndCallsUpdate()
    {
        var (viewModel, portfolioService, _, dialog) = CreateViewModel();
        var portfolio = new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 0 };
        dialog.OnShowPortfolioFormDialog = vm => vm.Name = "Growth";

        await viewModel.EditPortfolioAsync(portfolio);

        dialog.LastPortfolioFormDialog!.BrokerName.Should().Be("XPI");
        dialog.LastPortfolioFormDialog.CanChangeBroker.Should().BeFalse();
        portfolioService.LastUpdateRequest!.Value.BrokerName.Should().Be("XPI");
        portfolioService.LastUpdateRequest.Value.CurrentName.Should().Be("Default");
        portfolioService.LastUpdateRequest.Value.Request.Name.Should().Be("Growth");
    }

    [Fact]
    public async Task DeletePortfolioAsync_WithAssets_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, portfolioService, _, dialog) = CreateViewModel();
        var portfolio = new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 2 };

        await viewModel.DeletePortfolioAsync(portfolio);

        viewModel.ActionError.Should().Contain("still holds 2 asset(s)");
        dialog.LastConfirmMessage.Should().BeNull();
        portfolioService.LastDeleteRequest.Should().BeNull();
    }

    [Fact]
    public async Task DeletePortfolioAsync_EmptyUnderActiveBroker_ConfirmsThenDeletesWithActiveScope()
    {
        var (viewModel, portfolioService, _, _) = CreateViewModel();
        var portfolio = new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 0 };

        await viewModel.DeletePortfolioAsync(portfolio);

        portfolioService.LastDeleteRequest!.Value.BrokerName.Should().Be("XPI");
        portfolioService.LastDeleteRequest.Value.PortfolioName.Should().Be("Default");
        portfolioService.LastDeleteRequest.Value.Scope.Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public async Task DeletePortfolioAsync_EmptyUnderHistoricBroker_DeletesWithHistoricScope()
    {
        var (viewModel, portfolioService, _, _) = CreateViewModel();
        var portfolio = new PortfolioDTO { Name = "Old", BrokerName = "Avenue", BrokerStatus = "Historic", AssetCount = 0 };

        await viewModel.DeletePortfolioAsync(portfolio);

        portfolioService.LastDeleteRequest!.Value.Scope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public async Task DeletePortfolioAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, portfolioService, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var portfolio = new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 0 };

        await viewModel.DeletePortfolioAsync(portfolio);

        portfolioService.LastDeleteRequest.Should().BeNull();
    }
}
