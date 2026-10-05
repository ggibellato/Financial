using Financial.App.E2ETests.Infrastructure;
using FlaUI.Core.Definitions;
using FluentAssertions;

namespace Financial.App.E2ETests.Smoke;

[Trait("Category", "E2E")]
[Trait("Category", "Smoke")]
public class InvestmentAssetTests
{
    [Fact]
    public void InvestmentAsset_OpensWithSummary()
    {
        AppSession.Run(nameof(InvestmentAsset_OpensWithSummary), session =>
        {
            var window = session.Window;
            window.FindById("nav-active-investments").Press();

            window.ExpandTreeItem("XPI");
            window.ExpandTreeItem("Default");
            window.FindByNameStartingWith(ControlType.TreeItem, "BCIA11").Patterns.SelectionItem.Pattern.Select();

            window.FindById("asset-summary-name").Name.Should().Be("BCIA11");
        });
    }
}
