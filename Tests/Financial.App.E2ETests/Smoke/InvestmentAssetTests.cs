using Financial.App.E2ETests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FluentAssertions;

namespace Financial.App.E2ETests.Smoke;

[Trait("Category", "E2E")]
[Trait("Category", "Smoke")]
public class InvestmentAssetTests
{
    private const int SelectionAttempts = 3;
    private static readonly TimeSpan SummaryWaitPerAttempt = TimeSpan.FromSeconds(8);

    [Fact]
    public void InvestmentAsset_OpensWithSummary()
    {
        AppSession.Run(nameof(InvestmentAsset_OpensWithSummary), session =>
        {
            var window = session.Window;
            window.FindById("nav-active-investments").Press();

            SelectAssetUntilSummaryShows(window, "BCIA11");

            window.FindByIdNamed("asset-summary-name", "BCIA11").Name.Should().Be("BCIA11");
        });
    }

    private static void SelectAssetUntilSummaryShows(AutomationElement window, string assetName)
    {
        for (var attempt = 1; attempt <= SelectionAttempts; attempt++)
        {
            window.ExpandTreeItem("XPI");
            window.ExpandTreeItem("Default");
            window.SelectTreeItem(assetName);

            if (window.WaitForIdNamed("asset-summary-name", assetName, SummaryWaitPerAttempt))
            {
                return;
            }
        }
    }
}
