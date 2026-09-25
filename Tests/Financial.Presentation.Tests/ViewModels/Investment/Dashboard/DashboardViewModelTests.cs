using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Presentation.App.ViewModels.Investment;
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

    private static DataQualityReportDTO Report() => new()
    {
        UnpricedOpenHoldings = [new UnpricedOpenHoldingFinding("Chase", "Income", "VUSA")],
    };

    private static UpcomingIncomeDTO[] Income() =>
    [
        new("VUSA", "Chase", DateTime.Today.AddDays(-20), DateTime.Today.AddDays(10), 12m),
    ];

    private static (DashboardViewModel ViewModel, StubPortfolioDashboardService Dashboard, StubAllocationBreakdownService Allocation) CreateViewModel(
        StubPortfolioDashboardService? dashboardService = null,
        StubAllocationBreakdownService? allocationService = null,
        StubDataQualityReportService? reportService = null,
        StubUpcomingIncomeService? incomeService = null,
        FakeNavigationTree? activeTree = null,
        FakeNavigationTree? historicTree = null)
    {
        dashboardService ??= new StubPortfolioDashboardService { Dashboard = new PortfolioDashboardDTO { MarketValue = 1000m } };
        allocationService ??= new StubAllocationBreakdownService { Breakdown = Allocation() };
        reportService ??= new StubDataQualityReportService { Report = Report() };
        incomeService ??= new StubUpcomingIncomeService { Entries = Income() };
        var kpiTiles = new DashboardKpiTilesViewModel(dashboardService, new RecordingLogger<DashboardKpiTilesViewModel>());
        var allocation = new AllocationBreakdownViewModel(allocationService, new RecordingLogger<AllocationBreakdownViewModel>());
        var warnings = new DataQualityWarningsViewModel(reportService, new RecordingLogger<DataQualityWarningsViewModel>());
        var income = new UpcomingIncomeViewModel(incomeService, new RecordingLogger<UpcomingIncomeViewModel>());
        var viewModel = new DashboardViewModel(
            kpiTiles, allocation, warnings, income, activeTree ?? new FakeNavigationTree(), historicTree ?? new FakeNavigationTree());
        return (viewModel, dashboardService, allocationService);
    }

    [Fact]
    public void Constructor_LoadsEveryPanel()
    {
        var (vm, dashboardService, allocationService) = CreateViewModel();

        dashboardService.GetDashboardCallCount.Should().Be(1);
        allocationService.GetAllocationBreakdownCallCount.Should().Be(1);
        vm.KpiTiles.MarketValue.Should().Be(1000m);
        vm.Allocation.Entries.Should().ContainSingle();
        vm.Warnings.Categories.Should().ContainSingle();
        vm.Income.Entries.Should().ContainSingle();
        vm.ShowPanels.Should().BeTrue();
        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void EveryPanelFailing_ShowsThePageLevelErrorInsteadOfThePanels()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") },
            new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") },
            new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("thud") });

        vm.ShowPageLevelError.Should().BeTrue();
        vm.ShowPanels.Should().BeFalse();
    }

    [Fact]
    public void ThreeOfFourPanelsFailing_StillRendersEveryPanelInPlace()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") },
            new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") });

        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.Income.ShowContent.Should().BeTrue();
        vm.Income.Entries.Should().ContainSingle();
    }

    [Fact]
    public void OnlyTheWarningsPanelFailing_KeepsEveryPanelVisibleAndTheOtherPanelsContentIntact()
    {
        var (vm, _, _) = CreateViewModel(
            reportService: new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") });

        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.Warnings.HasError.Should().BeTrue();
        vm.KpiTiles.ShowContent.Should().BeTrue();
        vm.Allocation.ShowContent.Should().BeTrue();
        vm.Income.ShowContent.Should().BeTrue();
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
        vm.Income.Entries.Should().ContainSingle();
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
        vm.Income.ShowContent.Should().BeTrue();
    }

    [Fact]
    public void OnlyTheUpcomingIncomePanelFailing_KeepsEveryPanelVisibleAndTheOtherPanelsContentIntact()
    {
        var (vm, _, _) = CreateViewModel(
            incomeService: new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("thud") });

        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.Income.HasError.Should().BeTrue();
        vm.KpiTiles.MarketValue.Should().Be(1000m);
        vm.Allocation.Entries.Should().ContainSingle();
        vm.Warnings.Categories.Should().ContainSingle();
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
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") },
            new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") },
            new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("thud") });
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
        var reportService = new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") };
        var incomeService = new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("thud") };
        var (vm, _, _) = CreateViewModel(dashboardService, allocationService, reportService, incomeService);
        vm.ShowPageLevelError.Should().BeTrue();

        dashboardService.ThrowOnGetDashboard = null;
        dashboardService.Dashboard = new PortfolioDashboardDTO { MarketValue = 250m };
        allocationService.ThrowOnGetAllocationBreakdown = null;
        allocationService.Breakdown = Allocation();
        reportService.ThrowOnGenerateReport = null;
        reportService.Report = Report();
        incomeService.ThrowOnGetUpcomingIncome = null;
        incomeService.Entries = Income();
        await vm.LoadAllAsync();

        dashboardService.GetDashboardCallCount.Should().Be(2);
        allocationService.GetAllocationBreakdownCallCount.Should().Be(2);
        reportService.GenerateReportCallCount.Should().Be(2);
        incomeService.GetUpcomingIncomeCallCount.Should().Be(2);
        vm.ShowPageLevelError.Should().BeFalse();
        vm.ShowPanels.Should().BeTrue();
        vm.KpiTiles.MarketValue.Should().Be(250m);
        vm.Allocation.Entries.Should().ContainSingle();
        vm.Warnings.Categories.Should().ContainSingle();
        vm.Income.Entries.Should().ContainSingle();
    }

    [Fact]
    public void RetryAllCommand_RaisesRecoveredFromErrorOnceTheRetrySettlesWithSuccess()
    {
        var dashboardService = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var allocationService = new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") };
        var reportService = new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") };
        var incomeService = new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("thud") };
        var (vm, _, _) = CreateViewModel(dashboardService, allocationService, reportService, incomeService);
        var recovered = 0;
        vm.RecoveredFromError += (_, _) => recovered++;

        dashboardService.ThrowOnGetDashboard = null;
        dashboardService.Dashboard = new PortfolioDashboardDTO { MarketValue = 250m };
        allocationService.ThrowOnGetAllocationBreakdown = null;
        allocationService.Breakdown = Allocation();
        reportService.ThrowOnGenerateReport = null;
        reportService.Report = Report();
        incomeService.ThrowOnGetUpcomingIncome = null;
        incomeService.Entries = Income();
        vm.RetryAllCommand.Execute(null);

        recovered.Should().Be(1);
        vm.ShowPageLevelError.Should().BeFalse();
    }

    [Fact]
    public void RetryAllCommand_DoesNotRaiseRecoveredFromErrorWhenTheRetryStillFails()
    {
        var (vm, _, _) = CreateViewModel(
            new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") },
            new StubAllocationBreakdownService { ThrowOnGetAllocationBreakdown = new InvalidOperationException("bang") },
            new StubDataQualityReportService { ThrowOnGenerateReport = new InvalidOperationException("crash") },
            new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("thud") });
        var recovered = 0;
        vm.RecoveredFromError += (_, _) => recovered++;

        vm.RetryAllCommand.Execute(null);

        recovered.Should().Be(0);
        vm.ShowPageLevelError.Should().BeTrue();
    }

    [Fact]
    public void NavigateToHoldingCommand_SelectsInTheActiveTreeFirstAndAsksTheShellToShowIt()
    {
        var activeTree = new FakeNavigationTree { HoldsEverything = true };
        var historicTree = new FakeNavigationTree();
        var (vm, _, _) = CreateViewModel(activeTree: activeTree, historicTree: historicTree);
        var scopes = new List<InvestmentScope>();
        vm.NavigateToTreeRequested += (_, scope) => scopes.Add(scope);

        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));

        activeTree.Requested.Should().Equal(("Chase", "Income", "VUSA"));
        historicTree.Requested.Should().BeEmpty();
        scopes.Should().Equal(InvestmentScope.Active);
        vm.Warnings.NavigationError.Should().BeNull();
    }

    [Fact]
    public void NavigateToHoldingCommand_FallsThroughToTheHistoricTreeWhenTheActiveTreeMisses()
    {
        var activeTree = new FakeNavigationTree();
        var historicTree = new FakeNavigationTree { HoldsEverything = true };
        var (vm, _, _) = CreateViewModel(activeTree: activeTree, historicTree: historicTree);
        var scopes = new List<InvestmentScope>();
        vm.NavigateToTreeRequested += (_, scope) => scopes.Add(scope);

        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("XPI", "FII", "BBAS3"));

        activeTree.Requested.Should().Equal(("XPI", "FII", "BBAS3"));
        historicTree.Requested.Should().Equal(("XPI", "FII", "BBAS3"));
        scopes.Should().Equal(InvestmentScope.Historic);
        vm.Warnings.NavigationError.Should().BeNull();
    }

    [Fact]
    public void NavigateToHoldingCommand_WhenNeitherTreeHoldsIt_ReportsItInlineAndStaysPut()
    {
        var (vm, _, _) = CreateViewModel();
        var scopes = new List<InvestmentScope>();
        vm.NavigateToTreeRequested += (_, scope) => scopes.Add(scope);

        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));

        scopes.Should().BeEmpty();
        vm.Warnings.NavigationError.Should().Be(
            "Unable to locate VUSA — it may have moved or been archived since this report was generated.");
    }

    [Fact]
    public void NavigateToHoldingCommand_WithCorporateActionId_FocusesItOnTheResolvedTree()
    {
        var activeTree = new FakeNavigationTree { HoldsEverything = true };
        var (vm, _, _) = CreateViewModel(activeTree: activeTree);
        var corporateActionId = Guid.NewGuid();

        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("XPI", "FII", "BBAS3", corporateActionId));

        activeTree.AssetDetailsFake.FocusedCorporateActionId.Should().Be(corporateActionId);
    }

    [Fact]
    public void NavigateToHoldingCommand_WithoutCorporateActionId_DoesNotCallFocusCorporateAction()
    {
        var activeTree = new FakeNavigationTree { HoldsEverything = true };
        var (vm, _, _) = CreateViewModel(activeTree: activeTree);

        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));

        activeTree.AssetDetailsFake.FocusedCorporateActionId.Should().BeNull();
    }

    [Fact]
    public void NavigateToHoldingCommand_ClearsAnEarlierFailureOnceANavigationSucceeds()
    {
        var activeTree = new FakeNavigationTree();
        var (vm, _, _) = CreateViewModel(activeTree: activeTree);
        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));
        vm.Warnings.NavigationError.Should().NotBeNull();

        activeTree.HoldsEverything = true;
        vm.NavigateToHoldingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));

        vm.Warnings.NavigationError.Should().BeNull();
    }

    [Fact]
    public void AWarningRowSelection_RunsTheSameNavigationTheCommandDoes()
    {
        var activeTree = new FakeNavigationTree { HoldsEverything = true };
        var (vm, _, _) = CreateViewModel(activeTree: activeTree);
        var scopes = new List<InvestmentScope>();
        vm.NavigateToTreeRequested += (_, scope) => scopes.Add(scope);

        vm.Warnings.SelectFindingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));

        activeTree.Requested.Should().Equal(("Chase", "Income", "VUSA"));
        scopes.Should().Equal(InvestmentScope.Active);
    }

    [Fact]
    public void ViewMissingPriceHoldings_ExpandsTheMissingPriceCategoryInTheWarningsPanel()
    {
        var (vm, _, _) = CreateViewModel();

        vm.KpiTiles.ViewMissingPriceHoldingsCommand.Execute(null);

        vm.Warnings.Categories.Single(category => category.Category == DataQualityCategory.UnpricedOpenHoldings)
            .IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void Constructor_RejectsAMissingPanelViewModel()
    {
        var kpiTiles = new DashboardKpiTilesViewModel(
            new StubPortfolioDashboardService(), new RecordingLogger<DashboardKpiTilesViewModel>());
        var allocation = new AllocationBreakdownViewModel(
            new StubAllocationBreakdownService(), new RecordingLogger<AllocationBreakdownViewModel>());
        var warnings = new DataQualityWarningsViewModel(
            new StubDataQualityReportService(), new RecordingLogger<DataQualityWarningsViewModel>());
        var income = new UpcomingIncomeViewModel(
            new StubUpcomingIncomeService(), new RecordingLogger<UpcomingIncomeViewModel>());
        var tree = new FakeNavigationTree();

        var missingKpiTiles = () => new DashboardViewModel(null!, allocation, warnings, income, tree, tree);
        var missingAllocation = () => new DashboardViewModel(kpiTiles, null!, warnings, income, tree, tree);
        var missingWarnings = () => new DashboardViewModel(kpiTiles, allocation, null!, income, tree, tree);
        var missingIncome = () => new DashboardViewModel(kpiTiles, allocation, warnings, null!, tree, tree);
        var missingActiveTree = () => new DashboardViewModel(kpiTiles, allocation, warnings, income, null!, tree);
        var missingHistoricTree = () => new DashboardViewModel(kpiTiles, allocation, warnings, income, tree, null!);

        missingKpiTiles.Should().Throw<ArgumentNullException>();
        missingAllocation.Should().Throw<ArgumentNullException>();
        missingWarnings.Should().Throw<ArgumentNullException>();
        missingIncome.Should().Throw<ArgumentNullException>();
        missingActiveTree.Should().Throw<ArgumentNullException>();
        missingHistoricTree.Should().Throw<ArgumentNullException>();
    }
}

internal sealed class FakeNavigationTree : IMainNavigationViewModel
{
    public bool HoldsEverything { get; set; }

    public List<(string BrokerName, string PortfolioName, string AssetName)> Requested { get; } = [];

    public bool SelectHolding(string brokerName, string portfolioName, string assetName)
    {
        Requested.Add((brokerName, portfolioName, assetName));
        return HoldsEverything;
    }

    public MainNavigationViewModelBaseTests.SpyAssetDetailsViewModel AssetDetailsFake { get; } = new();
    public IAssetDetailsViewModel AssetDetails => AssetDetailsFake;
    public void ReloadSelectedNodeDetails() => throw new NotSupportedException();
    public bool CanAcceptDrop(TreeNodeViewModel? dragged, TreeNodeViewModel? target) => throw new NotSupportedException();
    public void HighlightDropTarget(TreeNodeViewModel? target) => throw new NotSupportedException();
    public Task DropAssetAsync(TreeNodeViewModel? dragged, TreeNodeViewModel? target) => throw new NotSupportedException();
}
