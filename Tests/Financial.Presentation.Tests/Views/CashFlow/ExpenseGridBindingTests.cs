using System.IO;
using System.Text.RegularExpressions;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;

namespace Financial.Presentation.Tests.Views.CashFlow;

/// <summary>
/// WPF's <c>{Binding X}</c> silently renders blank when <c>X</c> doesn't exist on the row's
/// DataContext - there is no compile error, unlike a typed binding in React/TypeScript. This is
/// exactly how ExpenseSectionView.xaml/CreditCardExpensesView.xaml ended up with stale
/// Category/CardTag column bindings that survived F02/F05's rename to CategoryName/CreditCardName
/// undetected until visual inspection. These tests parse each expense grid's column bindings and
/// assert every one resolves to a real <see cref="ExpenseDTO"/> property, so a future rename that
/// misses one of these two files fails a test instead of silently rendering a blank column.
/// </summary>
public class ExpenseGridBindingTests
{
    [Theory]
    [InlineData("ExpenseSectionView.xaml")]
    [InlineData("CreditCardExpensesView.xaml")]
    public void ExpenseDataGridColumns_BindOnlyToExistingExpenseDTOProperties(string xamlFileName)
    {
        var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Views", "CashFlow", xamlFileName);
        File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
        var xaml = File.ReadAllText(xamlPath);

        var dataGridMatch = Regex.Match(xaml, @"<DataGrid\s+[^>]*ItemsSource=""\{Binding (Filtered)?(Expenses|UnpaidCardCharges)\}""[\s\S]*?</DataGrid>");
        dataGridMatch.Success.Should().BeTrue($"expected to find the expense DataGrid in {xamlFileName}");

        var expenseDtoProperties = typeof(ExpenseDTO).GetProperties().Select(p => p.Name).ToHashSet();

        var boundProperties = Regex.Matches(dataGridMatch.Value, @"DataGridTextColumn\s+Binding=""\{Binding\s+([A-Za-z0-9_]+)")
            .Select(m => m.Groups[1].Value)
            .ToList();

        boundProperties.Should().NotBeEmpty($"expected at least one DataGridTextColumn binding in {xamlFileName}'s expense grid");
        boundProperties.Should().OnlyContain(
            p => expenseDtoProperties.Contains(p),
            $"every DataGridTextColumn in {xamlFileName}'s expense grid should bind to a real ExpenseDTO property");
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
