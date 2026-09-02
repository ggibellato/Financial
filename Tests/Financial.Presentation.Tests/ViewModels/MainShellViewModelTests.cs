using Financial.Presentation.App.ViewModels;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class MainShellViewModelTests
{
    private static Dictionary<string, object> BuildViewMap() => new()
    {
        ["active-investments"] = new object(),
        ["historic-investments"] = new object(),
        ["dividend-check"] = new object(),
        ["current-values"] = new object(),
        ["monthly"] = new object(),
        ["reserva"] = new object(),
        ["mensais"] = new object(),
        ["controle-mae"] = new object(),
        ["investment-snapshots"] = new object(),
        ["annual-summary"] = new object(),
        ["admin-assets"] = new object(),
        ["admin-brokers"] = new object(),
        ["admin-portfolios"] = new object(),
        ["admin-banks"] = new object(),
        ["admin-categories"] = new object(),
        ["admin-credit-cards"] = new object(),
        ["admin-income-sources"] = new object(),
        ["admin-investment-accounts"] = new object(),
        ["admin-recurring-bills"] = new object(),
        ["admin-reserve-buckets"] = new object(),
    };

    private static MainShellViewModel CreateShell(
        bool initialCollapsed,
        Action<bool> persistCollapsed,
        IReadOnlyDictionary<string, object> viewsByKey) =>
        new(initialCollapsed, persistCollapsed, viewsByKey,
            new SyncStatusViewModel(new StubCashFlowRepository(), new StubInvestmentRepository()),
            new PaymentDueBannerViewModel(new StubPaymentsDueService()));

    [Fact]
    public void Constructor_DefaultsToExpandedWhenInitialCollapsedIsFalse()
    {
        var views = BuildViewMap();
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: views);

        vm.IsCollapsed.Should().BeFalse();
        vm.SelectedChildId.Should().Be("active-investments");
        vm.SelectedContent.Should().BeSameAs(views["active-investments"]);
    }

    [Fact]
    public void Constructor_HonorsStoredCollapsedState()
    {
        var vm = CreateShell(initialCollapsed: true, persistCollapsed: _ => { }, viewsByKey: BuildViewMap());

        vm.IsCollapsed.Should().BeTrue();
    }

    [Fact]
    public void ToggleCollapsedCommand_FlipsStateAndInvokesPersistCallback()
    {
        var persisted = new List<bool>();
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: persisted.Add, viewsByKey: BuildViewMap());

        vm.ToggleCollapsedCommand.Execute(null);
        vm.IsCollapsed.Should().BeTrue();

        vm.ToggleCollapsedCommand.Execute(null);
        vm.IsCollapsed.Should().BeFalse();

        persisted.Should().Equal(true, false);
    }

    [Fact]
    public void SelectItemCommand_UpdatesSelectedContentAndChildId()
    {
        var views = BuildViewMap();
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: views);

        foreach (var (viewKey, view) in views)
        {
            vm.SelectItemCommand.Execute(viewKey);

            vm.SelectedChildId.Should().Be(viewKey);
            vm.SelectedContent.Should().BeSameAs(view);
        }
    }

    [Fact]
    public void SelectItemCommand_UnknownViewKey_DoesNotThrowOrChangeSelection()
    {
        var views = BuildViewMap();
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: views);
        var previousChildId = vm.SelectedChildId;
        var previousContent = vm.SelectedContent;

        Action act = () => vm.SelectItemCommand.Execute("unknown-key");

        act.Should().NotThrow();
        vm.SelectedChildId.Should().Be(previousChildId);
        vm.SelectedContent.Should().BeSameAs(previousContent);
    }

    [Fact]
    public void PropertyChanged_RaisedForIsCollapsedSelectedChildIdAndSelectedContent()
    {
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: BuildViewMap());
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        vm.ToggleCollapsedCommand.Execute(null);
        vm.SelectItemCommand.Execute("monthly");

        raised.Should().Contain(nameof(MainShellViewModel.IsCollapsed));
        raised.Should().Contain(nameof(MainShellViewModel.SelectedChildId));
        raised.Should().Contain(nameof(MainShellViewModel.SelectedContent));
    }

    [Fact]
    public void BreadcrumbText_DefaultsToFirstCategoryAndChild()
    {
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: BuildViewMap());

        vm.BreadcrumbText.Should().Be("Investments › Active Investments");
    }

    [Fact]
    public void BreadcrumbText_UpdatesWhenSelectedItemChanges()
    {
        var views = BuildViewMap();
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: views);

        foreach (var category in vm.Categories)
        {
            foreach (var child in category.Children)
            {
                vm.SelectItemCommand.Execute(child.ViewKey);

                vm.BreadcrumbText.Should().Be($"{category.Label} › {child.Label}");
            }
        }
    }

    [Fact]
    public void BreadcrumbText_GroupedSelection_ReturnsThreeSegmentPath()
    {
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: BuildViewMap());

        vm.SelectItemCommand.Execute("admin-brokers");

        vm.BreadcrumbText.Should().Be("Admin › Investment › Brokers");
    }

    [Fact]
    public void BreadcrumbText_FallsBackToEmDashForUnmatchedSelection()
    {
        var views = BuildViewMap();
        views["extra-key"] = new object();
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: views);

        vm.SelectItemCommand.Execute("extra-key");

        vm.BreadcrumbText.Should().Be("—");
    }

    [Fact]
    public void PropertyChanged_RaisedForBreadcrumbTextOnSelectionChange()
    {
        var vm = CreateShell(initialCollapsed: false, persistCollapsed: _ => { }, viewsByKey: BuildViewMap());
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        vm.SelectItemCommand.Execute("monthly");

        raised.Should().Contain(nameof(MainShellViewModel.BreadcrumbText));
    }
}
