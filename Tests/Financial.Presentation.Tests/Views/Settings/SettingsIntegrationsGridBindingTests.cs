using System.IO;
using System.Text.RegularExpressions;
using Financial.Presentation.App.ViewModels.Settings;
using FluentAssertions;

namespace Financial.Presentation.Tests.Views.Settings;

/// <summary>
/// WPF's <c>{Binding X}</c> silently renders blank when <c>X</c> doesn't exist on the row's
/// DataContext - no compile error, unlike a typed binding in React/TypeScript. Parses the sync
/// status DataGrid's column bindings and asserts every one resolves to a real
/// <see cref="CalendarSyncRow"/> property, following ExpenseGridBindingTests's pattern.
/// </summary>
public class SettingsIntegrationsGridBindingTests
{
    [Fact]
    public void SyncStatusDataGridColumns_BindOnlyToExistingCalendarSyncRowProperties()
    {
        var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Views", "Settings", "SettingsIntegrationsView.xaml");
        File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
        var xaml = File.ReadAllText(xamlPath);

        var dataGridMatch = Regex.Match(xaml, @"<DataGrid\s+[^>]*ItemsSource=""\{Binding SyncRows\}""[\s\S]*?</DataGrid>");
        dataGridMatch.Success.Should().BeTrue("expected to find the SyncRows DataGrid in SettingsIntegrationsView.xaml");

        var rowProperties = typeof(CalendarSyncRow).GetProperties().Select(p => p.Name).ToHashSet();

        var boundProperties = Regex.Matches(dataGridMatch.Value, @"DataGridTextColumn\s+Binding=""\{Binding\s+([A-Za-z0-9_]+)")
            .Select(m => m.Groups[1].Value)
            .ToList();

        boundProperties.Should().NotBeEmpty("expected at least one DataGridTextColumn binding in the sync status grid");
        boundProperties.Should().OnlyContain(
            p => rowProperties.Contains(p),
            "every DataGridTextColumn in the sync status grid should bind to a real CalendarSyncRow property");
    }

    [Fact]
    public void SettingsIntegrationsView_NeverUsesRunTextBinding()
    {
        var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Views", "Settings", "SettingsIntegrationsView.xaml");
        var xaml = File.ReadAllText(xamlPath);

        xaml.Should().NotContain("<Run Text=\"{Binding", "Run.Text defaults to TwoWay and crashes on a read-only bound property");
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
