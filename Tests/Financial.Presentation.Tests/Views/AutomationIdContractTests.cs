using Financial.TestUtilities;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Financial.Presentation.Tests.Views;

[Trait("Category", "Unit")]
public class AutomationIdContractTests
{
    private static readonly (string Xaml, string Id)[] RequiredIds =
    [
        ("MainWindow.xaml", "main-window"),
        ("MainWindow.xaml", "main-breadcrumb"),
        ("Components/NavigationView.xaml", "investment-tree"),
        ("Components/NavigationView.xaml", "asset-summary-name"),
        ("Views/CashFlow/MonthlyView.xaml", "monthly-tabs"),
        ("Views/CashFlow/MonthlyView.xaml", "monthly-error"),
        ("Views/CashFlow/MonthlyView.xaml", "monthly-retry"),
        ("Views/CashFlow/MonthlySummaryView.xaml", "monthly-category-grid"),
        ("Views/CashFlow/MonthlySummaryView.xaml", "monthly-category-total"),
        ("Views/CashFlow/BanksGridView.xaml", "monthly-banks-grid"),
        ("Views/CashFlow/ExpenseSectionView.xaml", "monthly-expenses-grid"),
        ("Views/CashFlow/ExpenseSectionView.xaml", "monthly-new-expense"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-date"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-description"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-description-error"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-card"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-payment-source"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-value"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-value-error"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-category"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-round-up"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-save"),
        ("Views/CashFlow/ExpenseFormView.xaml", "expense-form-cancel"),
    ];

    private static readonly Regex AutomationIdAttribute = new(@"AutomationProperties\.AutomationId=""([^""]*)""");
    private static readonly Regex KebabCase = new(@"^[a-z0-9]+(-[a-z0-9]+)*$");

    public static TheoryData<string, string> RequiredIdCases()
    {
        var data = new TheoryData<string, string>();
        foreach (var (xaml, id) in RequiredIds)
        {
            data.Add(xaml, id);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RequiredIdCases))]
    public void RequiredAutomationId_IsDeclaredExactlyOnceInItsXaml(string xaml, string id)
    {
        var declared = DeclaredIds(Path.Combine(FindAppRoot(), xaml));

        declared.Count(declaredId => declaredId == id).Should().Be(1, $"{xaml} must declare AutomationId '{id}' exactly once");
    }

    [Fact]
    public void NavigationButtons_AreIdentifiedByTheirNavigationItemId()
    {
        var sidebar = File.ReadAllText(Path.Combine(FindAppRoot(), "Components", "Sidebar.xaml"));

        var template = Regex.Match(sidebar, @"<DataTemplate x:Key=""NavChildButtonTemplate""[\s\S]*?</DataTemplate>").Value;

        template.Should().Contain("AutomationProperties.AutomationId=\"{Binding Id, StringFormat=nav-{0}}\"");
    }

    [Fact]
    public void InvestmentTreeItems_AreNamedByTheirDisplayName()
    {
        var navigationView = File.ReadAllText(Path.Combine(FindAppRoot(), "Components", "NavigationView.xaml"));

        navigationView.Should().Contain("<Setter Property=\"AutomationProperties.Name\" Value=\"{Binding DisplayName}\"/>");
    }

    [Fact]
    public void EveryLiteralAutomationId_FollowsTheKebabCaseConvention()
    {
        var offenders = Directory.EnumerateFiles(FindAppRoot(), "*.xaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(path => DeclaredIds(path).Where(id => !id.StartsWith('{') && !KebabCase.IsMatch(id)).Select(id => $"{Path.GetFileName(path)}: '{id}'"))
            .ToList();

        offenders.Should().BeEmpty("AutomationIds are <screen>-<element>[-<qualifier>] in kebab-case (docs/ui/wpf.md)");
    }

    private static IEnumerable<string> DeclaredIds(string xamlPath)
    {
        File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
        return AutomationIdAttribute.Matches(File.ReadAllText(xamlPath)).Select(match => match.Groups[1].Value);
    }

    private static string FindAppRoot() => Path.Combine(RepoRoot.Find(), "Financial.App");
}
