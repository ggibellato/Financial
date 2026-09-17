using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class DashboardViewModelTests
{
    private static (DashboardViewModel ViewModel, StubPortfolioDashboardService Service) CreateViewModel(
        StubPortfolioDashboardService? service = null)
    {
        service ??= new StubPortfolioDashboardService { Dashboard = new PortfolioDashboardDTO { MarketValue = 1000m } };
        var kpiTiles = new DashboardKpiTilesViewModel(service, new RecordingLogger<DashboardKpiTilesViewModel>());
        return (new DashboardViewModel(kpiTiles), service);
    }

    [Fact]
    public void Constructor_LoadsTheKpiTilesPanel()
    {
        var (vm, service) = CreateViewModel();

        service.GetDashboardCallCount.Should().Be(1);
        vm.KpiTiles.MarketValue.Should().Be(1000m);
        vm.ShowPanels.Should().BeTrue();
        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void EveryPanelFailing_ShowsThePageLevelErrorInsteadOfThePanels()
    {
        var service = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var (vm, _) = CreateViewModel(service);

        vm.ShowPageLevelError.Should().BeTrue();
        vm.ShowPanels.Should().BeFalse();
    }

    [Fact]
    public async Task PanelStillLoading_DoesNotShowThePageLevelError()
    {
        var gate = new TaskCompletionSource<PortfolioDashboardDTO>();
        var service = new StubPortfolioDashboardService { Gate = gate };
        var (vm, _) = CreateViewModel(service);

        vm.KpiTiles.IsLoading.Should().BeTrue();
        vm.ShowPageLevelError.Should().BeFalse();

        gate.SetResult(new PortfolioDashboardDTO());
        await vm.LoadAllAsync();

        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void PanelErrorState_RaisesPropertyChangedForThePageLevelErrorProperties()
    {
        var service = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var kpiTiles = new DashboardKpiTilesViewModel(service, new RecordingLogger<DashboardKpiTilesViewModel>());
        var vm = new DashboardViewModel(kpiTiles);
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.RetryAllCommand.Execute(null);

        changed.Should().Contain(nameof(DashboardViewModel.ShowPageLevelError));
        changed.Should().Contain(nameof(DashboardViewModel.ShowPanels));
    }

    [Fact]
    public async Task RetryAllCommand_ReloadsEveryPanel()
    {
        var service = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var (vm, _) = CreateViewModel(service);
        vm.ShowPageLevelError.Should().BeTrue();

        service.ThrowOnGetDashboard = null;
        service.Dashboard = new PortfolioDashboardDTO { MarketValue = 250m };
        await vm.LoadAllAsync();

        service.GetDashboardCallCount.Should().Be(2);
        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.KpiTiles.MarketValue.Should().Be(250m);
    }

    [Fact]
    public void RetryAllCommand_RaisesRecoveredFromErrorOnceTheRetrySettlesWithSuccess()
    {
        var service = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var (vm, _) = CreateViewModel(service);
        var recovered = 0;
        vm.RecoveredFromError += (_, _) => recovered++;

        service.ThrowOnGetDashboard = null;
        service.Dashboard = new PortfolioDashboardDTO { MarketValue = 250m };
        vm.RetryAllCommand.Execute(null);

        recovered.Should().Be(1);
        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void RetryAllCommand_DoesNotRaiseRecoveredFromErrorWhenTheRetryStillFails()
    {
        var service = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var (vm, _) = CreateViewModel(service);
        var recovered = 0;
        vm.RecoveredFromError += (_, _) => recovered++;

        vm.RetryAllCommand.Execute(null);

        recovered.Should().Be(0);
        vm.ShowPageLevelError.Should().BeTrue();
    }

    [Fact]
    public void Constructor_RejectsAMissingPanelViewModel()
    {
        var act = () => new DashboardViewModel(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
