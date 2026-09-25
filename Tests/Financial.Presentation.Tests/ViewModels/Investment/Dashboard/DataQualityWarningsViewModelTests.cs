using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class DataQualityWarningsViewModelTests
{
    private static DataQualityReportDTO FullReport() => new()
    {
        SalesExceedPurchases =
        [
            new SalesExceedPurchasesFinding("Trading212", "Growth", "AAPL", new DateTime(2026, 3, 4), 2m, 5m),
        ],
        UnpricedOpenHoldings =
        [
            new UnpricedOpenHoldingFinding("Chase", "Income", "VUSA"),
        ],
        OpenHoldingsMissingCostBasis =
        [
            new OpenHoldingMissingCostBasisFinding("Chase", "Income", "VWRL"),
        ],
        StaleValuationCount = 3,
        UnresolvedTaxClassifications =
        [
            new UnresolvedTaxClassificationFinding("XPI", "FII", "BBAS3", "2025/26", EventCategory.Dividend),
        ],
        CorporateActionsAwaitingTaxReview =
        [
            new CorporateActionAwaitingTaxReviewFinding(
                "XPI", "FII", "PETR4", CorporateActionId, CorporateAction.CorporateActionType.Merger, new DateTime(2026, 4, 1), "2025/26"),
        ],
    };

    private static readonly Guid CorporateActionId = Guid.NewGuid();

    private static (DataQualityWarningsViewModel ViewModel, StubDataQualityReportService Service) CreateViewModel(
        StubDataQualityReportService? service = null)
    {
        service ??= new StubDataQualityReportService { Report = FullReport() };
        var viewModel = new DataQualityWarningsViewModel(service, new RecordingLogger<DataQualityWarningsViewModel>());
        return (viewModel, service);
    }

    [Fact]
    public async Task LoadAsync_BuildsOneCategoryPerNonZeroCategoryInSeverityOrder()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.LoadAsync();

        viewModel.Categories.Select(category => category.Category).Should().Equal(
            DataQualityCategory.SalesExceedPurchases,
            DataQualityCategory.UnpricedOpenHoldings,
            DataQualityCategory.OpenHoldingsMissingCostBasis,
            DataQualityCategory.StaleValuation,
            DataQualityCategory.UnresolvedTaxClassifications,
            DataQualityCategory.CorporateActionAwaitingTaxReview);
        viewModel.Categories.Select(category => category.Label).Should().Equal(
            "Impossible cash-flow sequence",
            "Missing price",
            "Missing cost basis",
            "Stale valuation",
            "Unresolved tax classification",
            "Corporate action awaiting tax review");
        viewModel.ShowCategories.Should().BeTrue();
        viewModel.ShowAllClear.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_OmitsEveryZeroCountCategory()
    {
        var (viewModel, _) = CreateViewModel(new StubDataQualityReportService
        {
            Report = new DataQualityReportDTO
            {
                UnpricedOpenHoldings = [new UnpricedOpenHoldingFinding("Chase", "Income", "VUSA")],
            },
        });

        await viewModel.LoadAsync();

        viewModel.Categories.Should().ContainSingle();
        viewModel.Categories[0].Category.Should().Be(DataQualityCategory.UnpricedOpenHoldings);
        viewModel.Categories[0].HeaderText.Should().Be("Missing price (1)");
    }

    [Fact]
    public async Task LoadAsync_WithNothingToReport_ShowsTheAllClearConfirmation()
    {
        var (viewModel, _) = CreateViewModel(new StubDataQualityReportService { Report = new DataQualityReportDTO() });

        await viewModel.LoadAsync();

        viewModel.Categories.Should().BeEmpty();
        viewModel.IsAllClear.Should().BeTrue();
        viewModel.ShowAllClear.Should().BeTrue();
        viewModel.ShowCategories.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_RendersStaleValuationAsACountOnlyEntry()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.LoadAsync();

        var stale = viewModel.Categories.Single(category => category.Category == DataQualityCategory.StaleValuation);
        stale.Findings.Should().BeEmpty();
        stale.HasFindings.Should().BeFalse();
        stale.IsCountOnly.Should().BeTrue();
        stale.Count.Should().Be(3);
        stale.HeaderText.Should().Be("Stale valuation (3)");
    }

    [Fact]
    public async Task LoadAsync_BuildsFindingRowsMatchingTheReactSecondaryText()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.LoadAsync();

        var sales = viewModel.Categories.Single(category => category.Category == DataQualityCategory.SalesExceedPurchases);
        sales.Findings.Should().ContainSingle();
        sales.Findings[0].AssetName.Should().Be("AAPL");
        sales.Findings[0].Holding.Should().Be(new WarningHoldingRef("Trading212", "Growth", "AAPL"));
        sales.Findings[0].SecondaryText.Should().Be("Growth · Trading212 — sold 5.00 more than held on 04/03/2026 (held 2.00)");

        var price = viewModel.Categories.Single(category => category.Category == DataQualityCategory.UnpricedOpenHoldings);
        price.Findings[0].SecondaryText.Should().Be("Income · Chase");

        var tax = viewModel.Categories.Single(category => category.Category == DataQualityCategory.UnresolvedTaxClassifications);
        tax.Findings[0].SecondaryText.Should().Be("FII · XPI — Dividend, tax year 2025/26");

        var corporateAction = viewModel.Categories.Single(category => category.Category == DataQualityCategory.CorporateActionAwaitingTaxReview);
        corporateAction.Findings.Should().ContainSingle();
        corporateAction.Findings[0].SecondaryText.Should().Be("FII · XPI — Merger, tax year 2025/26");
        corporateAction.Findings[0].Holding.Should().Be(new WarningHoldingRef("XPI", "FII", "PETR4", CorporateActionId));
    }

    [Fact]
    public async Task ExpandCategory_ExpandsThatCategoryAndAnnouncesIt()
    {
        var (viewModel, _) = CreateViewModel();
        await viewModel.LoadAsync();
        var expanded = new List<DataQualityCategory>();
        viewModel.CategoryExpanded += (_, category) => expanded.Add(category);

        viewModel.ExpandCategory(DataQualityCategory.UnpricedOpenHoldings);

        viewModel.Categories.Single(category => category.Category == DataQualityCategory.UnpricedOpenHoldings)
            .IsExpanded.Should().BeTrue();
        viewModel.Categories.Where(category => category.Category != DataQualityCategory.UnpricedOpenHoldings)
            .Should().OnlyContain(category => !category.IsExpanded);
        expanded.Should().Equal(DataQualityCategory.UnpricedOpenHoldings);
    }

    [Fact]
    public async Task ExpandCategory_ForACategoryWithNothingToExpand_DoesNothing()
    {
        var (viewModel, _) = CreateViewModel();
        await viewModel.LoadAsync();
        var expanded = 0;
        viewModel.CategoryExpanded += (_, _) => expanded++;

        viewModel.ExpandCategory(DataQualityCategory.StaleValuation);

        viewModel.Categories.Should().OnlyContain(category => !category.IsExpanded);
        expanded.Should().Be(0);
    }

    [Fact]
    public async Task SelectFindingCommand_RaisesNavigateToHoldingRequestedWithThatRowsHolding()
    {
        var (viewModel, _) = CreateViewModel();
        await viewModel.LoadAsync();
        var requested = new List<WarningHoldingRef>();
        viewModel.NavigateToHoldingRequested += (_, holding) => requested.Add(holding);

        viewModel.SelectFindingCommand.Execute(new WarningHoldingRef("Chase", "Income", "VUSA"));

        requested.Should().Equal(new WarningHoldingRef("Chase", "Income", "VUSA"));
    }

    [Fact]
    public void NavigationError_DrivesTheInfoBarsTwoWayOpenState()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.NavigationError = "Unable to locate VUSA";

        viewModel.HasNavigationError.Should().BeTrue();

        viewModel.HasNavigationError = false;

        viewModel.NavigationError.Should().BeNull();
    }

    [Fact]
    public async Task RefreshCommand_ClearsAnyOutstandingNavigationError()
    {
        var (viewModel, service) = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.NavigationError = "Unable to locate VUSA";

        viewModel.RefreshCommand.Execute(null);

        viewModel.NavigationError.Should().BeNull();
        service.GenerateReportCallCount.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_WhenTheServiceFails_SetsTheErrorStateAndHidesContent()
    {
        var (viewModel, _) = CreateViewModel(new StubDataQualityReportService
        {
            ThrowOnGenerateReport = new InvalidOperationException("boom"),
        });

        await viewModel.LoadAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("boom");
        viewModel.ShowContent.Should().BeFalse();
        viewModel.ShowCategories.Should().BeFalse();
        viewModel.ShowAllClear.Should().BeFalse();
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        var missingService = () => new DataQualityWarningsViewModel(null!, new RecordingLogger<DataQualityWarningsViewModel>());
        var missingLogger = () => new DataQualityWarningsViewModel(new StubDataQualityReportService(), null!);

        missingService.Should().Throw<ArgumentNullException>();
        missingLogger.Should().Throw<ArgumentNullException>();
    }
}

internal sealed class StubDataQualityReportService : IDataQualityReportService
{
    public DataQualityReportDTO Report { get; set; } = new();

    public Exception? ThrowOnGenerateReport { get; set; }

    public int GenerateReportCallCount { get; private set; }

    public DataQualityReportDTO GenerateReport()
    {
        GenerateReportCallCount++;

        if (ThrowOnGenerateReport is not null)
        {
            throw ThrowOnGenerateReport;
        }

        return Report;
    }
}
