using System.IO;
using System.Text.RegularExpressions;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.Views.Investment;

/// <summary>
/// AssetDetailsViewModel.CorporateActionsTabIndex is a magic number with no compiler-enforced link
/// to NavigationView.xaml's TabItem order - a future reordering would silently break dashboard
/// deep-link focus with no build error. Pins that link the same way
/// TransactionsAndCreditsGridBindingTests pins DataGrid column bindings to real DTO properties.
/// </summary>
public class NavigationViewTabOrderTests
{
    [Fact]
    public void CorporateActionsTabIndex_MatchesItsPositionInNavigationViewXaml()
    {
        var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Components", "NavigationView.xaml");
        File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
        var xaml = File.ReadAllText(xamlPath);

        var tabControlMatch = Regex.Match(xaml, @"<TabControl\s[\s\S]*?</TabControl>");
        tabControlMatch.Success.Should().BeTrue("expected to find the asset-detail TabControl in NavigationView.xaml");

        var headers = Regex.Matches(tabControlMatch.Value, @"<TabItem\s+Header=""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToList();

        headers.Should().NotBeEmpty();
        headers.IndexOf("Corporate Actions").Should().Be(
            AssetDetailsViewModel.CorporateActionsTabIndex,
            "AssetDetailsViewModel.FocusCorporateAction assumes this exact tab position");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Financial.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (Financial.slnx not found in any ancestor directory).");
    }
}
