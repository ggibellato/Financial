using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class DashboardViewModelTests
{
    private static AllocationBreakdownDTO Allocation() => new()
    {
        ByClass = [new AssetClassAllocationEntryDTO(Financial.Investment.Domain.Entities.GlobalAssetClass.Equity, 500m, 100m)],
    };

    private static (DashboardViewModel ViewModel, StubPortfolioDashboardService Dashboard, StubAllocationBreakdownService Allocation) CreateViewModel(
        StubPortfolioDashboardService? dashboardService = null,
        StubAllocationBreakdownService? allocationService = null)
    {
        dashboardService ??= new StubPortfolioDashboardService { Dashboard = new PortfolioDashboardDTO { MarketValue = 1000m } };
        allocationService ??= new StubAllocationBreakdownService { Breakdown = Allocation() };
        var kpiTiles = new DashboardKpiTilesViewModel(dashboardService, new RecordingLogger<DashboardKpiTilesViewModel>());
        var allocation = new AllocationBreakdownViewModel(allocationService, new RecordingLogger<AllocationBreakdownViewModel>());
        return (new DashboardViewModel(kpiTiles, allocation), dashboardService, allocationService);
    }

    [Fact]
    public void Constructor_LoadsEveryPanel()
    {
        var (vm, dashboardService, allocationService) = CreateViewModel();

        dashboardService.GetDashboardCallCount.Should().Be(1);
        allocationService.GetAllocationBreakdownCallCount.Should().Be(1);
        vm.KpiTiles.MarketValue.Should().Be(1000m);
        vm.Allocation.Entries.Should().ContainSingle();
        vm.ShowPanels.Should().BeTrue();
        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void EveryPanelFailing_ShowsThePageLevelErrorInsteadOfThePanels()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") });

        vm.ShowPageLevelError.Should().BeTrue();
        vm.ShowPanels.Should().BeFalse();
    }

    [Fact]
    public void OnlyTheKpiPanelFailing_KeepsEveryPanelVisibleAndTheOtherPanelsContentIntact()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") });

        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.KpiTiles.HasError.Should().BeTrue();
        vm.Allocation.ShowContent.Should().BeTrue();
        vm.Allocation.Entries.Should().ContainSingle();
    }

    [Fact]
    public void OnlyTheAllocationPanelFailing_KeepsEveryPanelVisibleAndTheOtherPanelsContentIntact()
    {
        var (vm, _, _) = CreateViewModel(
            allocationService: new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") });

        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.Allocation.HasError.Should().BeTrue();
        vm.KpiTiles.ShowContent.Should().BeTrue();
        vm.KpiTiles.MarketValue.Should().Be(1000m);
    }

    [Fact]
    public async Task PanelStillLoading_DoesNotShowThePageLevelError()
    {
        var gate = new TaskCompletionSource<PortfolioDashboardDTO>();
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { Gate = gate },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") });

        vm.KpiTiles.IsLoading.Should().BeTrue();
        vm.ShowPageLevelError.Should().BeFalse();

        gate.SetResult(new PortfolioDashboardDTO());
        await vm.LoadAllAsync();

        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void PanelErrorState_RaisesPropertyChangedForThePageLevelErrorProperties()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") });
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.RetryAllCommand.Execute(null);

        changed.Should().Contain(nameof(DashboardViewModel.ShowPageLevelError));
        changed.Should().Contain(nameof(DashboardViewModel.ShowPanels));
    }

    [Fact]
    public async Task RetryAllCommand_ReloadsEveryPanel()
    {
        var dashboardService = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var allocationService = new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") };
        var (vm, _, _) = CreateViewModel(dashboardService, allocationService);
        vm.ShowPageLevelError.Should().BeTrue();

        dashboardService.ThrowOnGetDashboard = null;
        dashboardService.Dashboard = new PortfolioDashboardDTO { MarketValue = 250m };
        allocationService.ThrowOnGetAllocationBreakdown = null;
        allocationService.Breakdown = Allocation();
        await vm.LoadAllAsync();

        dashboardService.GetDashboardCallCount.Should().Be(2);
        allocationService.GetAllocationBreakdownCallCount.Should().Be(2);
        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.KpiTiles.MarketValue.Should().Be(250m);
        vm.Allocation.Entries.Should().ContainSingle();
    }

    [Fact]
    public void RetryAllCommand_RaisesRecoveredFromErrorOnceTheRetrySettlesWithSuccess()
    {
        var dashboardService = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var allocationService = new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") };
        var (vm, _, _) = CreateViewModel(dashboardService, allocationService);
        var recovered = 0;
        vm.RecoveredFromError += (_, _) => recovered++;

        dashboardService.ThrowOnGetDashboard = null;
        dashboardService.Dashboard = new PortfolioDashboardDTO { MarketValue = 250m };
        allocationService.ThrowOnGetAllocationBreakdown = null;
        allocationService.Breakdown = Allocation();
        vm.RetryAllCommand.Execute(null);

        recovered.Should().Be(1);
        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void RetryAllCommand_DoesNotRaiseRecoveredFromErrorWhenTheRetryStillFails()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") });
        var recovered = 0;
        vm.RecoveredFromError += (_, _) => recovered++;

        vm.RetryAllCommand.Execute(null);

        recovered.Should().Be(0);
        vm.ShowPageLevelError.Should().BeTrue();
    }

    [Fact]
    public void Constructor_RejectsAMissingPanelViewModel()
    {
        var kpiTiles = new DashboardKpiTilesViewModel(
            new StubPortfolioDashboardService(), new RecordingLogger<DashboardKpiTilesViewModel>());
        var allocation = new AllocationBreakdownViewModel(
            new StubAllocationBreakdownService(), new RecordingLogger<AllocationBreakdownViewModel>());

        var missingKpiTiles = () => new DashboardViewModel(null!, allocation);
        var missingAllocation = () => new DashboardViewModel(kpiTiles, null!);

        missingKpiTiles.Should().Throw<ArgumentNullException>();
        missingAllocation.Should().Throw<ArgumentNullException>();
    }
}
