using Financial.App.E2ETests.Infrastructure;
using FlaUI.Core.Definitions;
using FluentAssertions;

namespace Financial.App.E2ETests.Smoke;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
[Trait("Category", "Smoke")]
public class AppStartsTests
{
    [Fact]
    public void App_Starts_ShowsMainWindowAndInvestmentTree()
    {
        AppSession.Run(nameof(App_Starts_ShowsMainWindowAndInvestmentTree), session =>
        {
            var window = session.Window;

            window.Title.Should().Be("Financial tools");
            window.AutomationId.Should().Be("main-window");

            window.FindById("nav-active-investments").Press();

            window.FindById("investment-tree");
            window.FindByNameStartingWith(ControlType.TreeItem, "XPI").Name.Should().StartWith("XPI");
        });
    }
}
