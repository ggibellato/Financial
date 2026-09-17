using System.IO;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.DependencyInjection;
using Financial.Investment.Application.Enums;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Presentation.App.Navigation;
using Financial.Presentation.App.Services;
using Financial.Presentation.App.ViewModels.Investment;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Presentation.Tests.Integration;

// Runs against a throwaway copy of the committed fixture, never the live data file.
public class DashboardCompositionTests : IDisposable
{
    private readonly string _dataFile;
    private readonly ServiceProvider _provider;

    public DashboardCompositionTests()
    {
        _dataFile = Path.Combine(Path.GetTempPath(), $"dashboard-composition-{Guid.NewGuid()}.json");
        File.Copy(TestDataPaths.DataJsonFile, _dataFile);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Investment:Repository:Provider"] = "LocalJson",
                ["Investment:DataJsonFile"] = _dataFile,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITelemetryTracer>(new RecordingTelemetryTracer());
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        services.AddFinancialApplication();
        services.AddFinancialInfrastructure(configuration);
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<DashboardKpiTilesViewModel>();
        services.AddTransient<AllocationBreakdownViewModel>();
        services.AddTransient<DataQualityWarningsViewModel>();
        services.AddTransient<UpcomingIncomeViewModel>();
        services.AddTransient<MainNavigationViewModel>();
        services.AddTransient<MainNavigationViewModelHistoric>();

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    [Trait("AC", "P52-F06-wpf-portfolio-dashboard-01")]
    public void DashboardNavEntryAppearsFirstUnderInvestments()
    {
        var investments = NavTree.Categories.Single(category => category.Id == "investments");

        investments.Children[0].Id.Should().Be("dashboard");
        investments.Children[0].Label.Should().Be("Dashboard");
        investments.Children[0].ViewKey.Should().Be("dashboard");
    }

    [Fact]
    [Trait("AC", "P52-F06-wpf-portfolio-dashboard-02")]
    public async Task AllFourPanelViewModelsResolveAndLoadFromTheRealDependencyGraph()
    {
        var dashboard = BuildDashboard();

        await dashboard.LoadAllAsync();

        dashboard.ShowPageLevelError.Should().BeFalse();
        dashboard.ShowPanels.Should().BeTrue();
        dashboard.KpiTiles.ShowContent.Should().BeTrue();
        dashboard.Allocation.ShowContent.Should().BeTrue();
        dashboard.Warnings.ShowContent.Should().BeTrue();
        dashboard.Income.ShowContent.Should().BeTrue();
    }

    [Fact]
    [Trait("AC", "P52-F06-wpf-portfolio-dashboard-02")]
    public async Task EveryPanelsFiguresComeFromTheSameFixtureTheReactSuiteDocuments()
    {
        var dashboard = BuildDashboard();

        await dashboard.LoadAllAsync();

        dashboard.KpiTiles.Invested.Should().Be(800m);
        dashboard.KpiTiles.RealisedGainLoss.Should().Be(-30m);
        dashboard.KpiTiles.IncomeLifetime.Should().Be(11m);
        dashboard.KpiTiles.IsPartial.Should().BeTrue();
        dashboard.KpiTiles.UnvaluedHoldingCount.Should().Be(1);

        dashboard.Allocation.Entries.Sum(entry => entry.Percentage).Should().BeInRange(0m, 100m);

        dashboard.Warnings.Categories.Should().Contain(category =>
            category.Category == DataQualityCategory.UnpricedOpenHoldings && category.Count == 1);

        dashboard.Income.SelectedWindow.Should().Be(UpcomingIncomeWindow.Days90);
    }

    [Fact]
    [Trait("AC", "P52-F06-wpf-portfolio-dashboard-03")]
    public async Task AWarningFindingNavigatesToItsHoldingInTheRealTreeBuiltFromTheSameFixture()
    {
        var activeTree = _provider.GetRequiredService<MainNavigationViewModel>();
        var historicTree = _provider.GetRequiredService<MainNavigationViewModelHistoric>();
        await activeTree.LoadNavigationTreeAsync();
        await historicTree.LoadNavigationTreeAsync();
        var dashboard = BuildDashboard(activeTree, historicTree);
        await dashboard.LoadAllAsync();
        var scopes = new List<InvestmentScope>();
        dashboard.NavigateToTreeRequested += (_, scope) => scopes.Add(scope);

        var finding = dashboard.Warnings.Categories
            .Single(category => category.Category == DataQualityCategory.UnpricedOpenHoldings)
            .Findings[0];
        dashboard.Warnings.SelectFindingCommand.Execute(finding.Holding);

        scopes.Should().Equal(InvestmentScope.Active);
        dashboard.Warnings.NavigationError.Should().BeNull();
        activeTree.SelectedNode!.GetMetadata<string>(NavigationMetadataKeys.AssetName)
            .Should().Be(finding.Holding.AssetName);
        activeTree.SelectedNode.IsSelected.Should().BeTrue();
    }

    public void Dispose()
    {
        _provider.Dispose();

        if (File.Exists(_dataFile))
        {
            File.Delete(_dataFile);
        }

        GC.SuppressFinalize(this);
    }

    private DashboardViewModel BuildDashboard(
        IMainNavigationViewModel? activeTree = null,
        IMainNavigationViewModel? historicTree = null) => new(
            _provider.GetRequiredService<DashboardKpiTilesViewModel>(),
            _provider.GetRequiredService<AllocationBreakdownViewModel>(),
            _provider.GetRequiredService<DataQualityWarningsViewModel>(),
            _provider.GetRequiredService<UpcomingIncomeViewModel>(),
            activeTree ?? _provider.GetRequiredService<MainNavigationViewModel>(),
            historicTree ?? _provider.GetRequiredService<MainNavigationViewModelHistoric>());
}
