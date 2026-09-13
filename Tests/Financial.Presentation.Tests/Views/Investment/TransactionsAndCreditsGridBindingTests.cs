using System.IO;
using System.Text.RegularExpressions;
using Financial.Investment.Application.DTOs;
using FluentAssertions;

namespace Financial.Presentation.Tests.Views.Investment;

/// <summary>
/// WPF's <c>{Binding X}</c> silently renders blank when <c>X</c> doesn't exist on the row's
/// DataContext - no compile error, unlike a typed binding in React/TypeScript. Parses the
/// transactions/credits DataGrid column bindings (including the FX provenance column's
/// MultiBinding paths) and asserts every one resolves to a real DTO property, following
/// ExpenseGridBindingTests's pattern.
/// </summary>
public class TransactionsAndCreditsGridBindingTests
{
    [Fact]
    public void TransactionsDataGridColumns_BindOnlyToExistingTransactionDTOProperties()
    {
        var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Views", "Investment", "TransactionsView.xaml");
        File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
        var xaml = File.ReadAllText(xamlPath);

        var dataGridMatch = Regex.Match(xaml, @"<DataGrid\s+[^>]*ItemsSource=""\{Binding AssetDetails\.Transactions\.Transactions\}""[\s\S]*?</DataGrid>");
        dataGridMatch.Success.Should().BeTrue("expected to find the Transactions DataGrid in TransactionsView.xaml");

        AssertBoundPropertiesExist(dataGridMatch.Value, typeof(TransactionDTO));
    }

    [Fact]
    public void CreditsDataGridColumns_BindOnlyToExistingCreditDTOProperties()
    {
        var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Views", "Investment", "CreditsView.xaml");
        File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
        var xaml = File.ReadAllText(xamlPath);

        var dataGridMatch = Regex.Match(xaml, @"<DataGrid\s+[^>]*ItemsSource=""\{Binding AssetDetails\.Credits\.Credits\}""[\s\S]*?</DataGrid>");
        dataGridMatch.Success.Should().BeTrue("expected to find the Credits DataGrid in CreditsView.xaml");

        AssertBoundPropertiesExist(dataGridMatch.Value, typeof(CreditDTO));
    }

    private static void AssertBoundPropertiesExist(string dataGridXaml, Type dtoType)
    {
        var dtoProperties = dtoType.GetProperties().Select(p => p.Name).ToHashSet();

        var boundProperties = Regex.Matches(dataGridXaml, @"DataGridTextColumn\s+Binding=""\{Binding\s+([A-Za-z0-9_]+)")
            .Select(m => m.Groups[1].Value)
            .Concat(Regex.Matches(dataGridXaml, @"<Binding Path=""([A-Za-z0-9_]+)""\s*/>")
                .Select(m => m.Groups[1].Value))
            .ToList();

        boundProperties.Should().NotBeEmpty($"expected at least one column binding in the {dtoType.Name} grid");
        boundProperties.Should().OnlyContain(
            p => dtoProperties.Contains(p),
            $"every column binding in the {dtoType.Name} grid should bind to a real {dtoType.Name} property");
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
