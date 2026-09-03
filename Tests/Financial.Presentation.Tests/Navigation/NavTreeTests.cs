using Financial.Presentation.App.Navigation;
using FluentAssertions;

namespace Financial.Presentation.Tests.Navigation;

public class NavTreeTests
{
    [Fact]
    public void Categories_HasExactlyFourCategories()
    {
        NavTree.Categories.Should().HaveCount(4);
        NavTree.Categories[0].Id.Should().Be("investments");
        NavTree.Categories[1].Id.Should().Be("cashflow");
        NavTree.Categories[2].Id.Should().Be("admin");
        NavTree.Categories[3].Id.Should().Be("settings");
    }

    [Fact]
    public void SettingsCategory_HasOneChildAppearance()
    {
        var settings = NavTree.Categories.Single(c => c.Id == "settings");

        settings.Label.Should().Be("Settings");
        settings.Groups.Should().BeNull();
        settings.Children.Should().ContainSingle();
        settings.Children[0].Id.Should().Be("appearance");
        settings.Children[0].Label.Should().Be("Appearance");
        settings.Children[0].ViewKey.Should().Be("settings-appearance");
    }

    [Fact]
    public void InvestmentsCategory_HasFourChildrenInExistingTabOrder()
    {
        var investments = NavTree.Categories.Single(c => c.Id == "investments");

        investments.Label.Should().Be("Investments");
        investments.Children.Select(c => (c.Id, c.Label, c.ViewKey)).Should().Equal(
            ("active-investments", "Active Investments", "active-investments"),
            ("historic-investments", "Historic Investments", "historic-investments"),
            ("dividend-check", "Shares Dividend check", "dividend-check"),
            ("current-values", "Read Assets current values", "current-values"));
    }

    [Fact]
    public void CashFlowCategory_HasSixChildrenInExistingTabOrder()
    {
        var cashflow = NavTree.Categories.Single(c => c.Id == "cashflow");

        cashflow.Label.Should().Be("CashFlow");
        cashflow.Children.Select(c => (c.Id, c.Label, c.ViewKey)).Should().Equal(
            ("monthly", "Monthly", "monthly"),
            ("reserva", "Reserva", "reserva"),
            ("mensais", "Mensais", "mensais"),
            ("controle-mae", "Controle Mae", "controle-mae"),
            ("investment-snapshots", "Investment Snapshots", "investment-snapshots"),
            ("annual-summary", "Annual Summary", "annual-summary"));
    }

    [Fact]
    public void AdminCategory_HasNoDirectChildrenAndExactlyTwoGroupsInAcOrder()
    {
        var admin = NavTree.Categories.Single(c => c.Id == "admin");

        admin.Label.Should().Be("Admin");
        admin.Children.Should().BeEmpty();
        admin.Groups.Should().NotBeNull();
        admin.Groups!.Select(g => g.Id).Should().Equal("investment", "cashflow");
    }

    [Fact]
    public void AdminInvestmentGroup_HasAssetsBrokersPortfoliosInAcOrder()
    {
        var investmentGroup = NavTree.Categories.Single(c => c.Id == "admin").Groups!.Single(g => g.Id == "investment");

        investmentGroup.Label.Should().Be("Investment");
        investmentGroup.Children.Select(c => c.Label).Should().Equal("Assets", "Brokers", "Portfolios");
    }

    [Fact]
    public void AdminCashFlowGroup_HasSevenEntitiesInAcOrder()
    {
        var cashflowGroup = NavTree.Categories.Single(c => c.Id == "admin").Groups!.Single(g => g.Id == "cashflow");

        cashflowGroup.Label.Should().Be("CashFlow");
        cashflowGroup.Children.Select(c => c.Label).Should().Equal(
            "Banks", "Categories", "Credit Cards", "Income Sources", "Investment Accounts", "Recurring Bills", "Reserve Buckets");
    }

    [Fact]
    public void AllChildViewKeys_AreUnique()
    {
        var directViewKeys = NavTree.Categories.SelectMany(c => c.Children).Select(c => c.ViewKey);
        var groupedViewKeys = NavTree.Categories
            .SelectMany(c => c.Groups ?? [])
            .SelectMany(g => g.Children)
            .Select(c => c.ViewKey);
        var viewKeys = directViewKeys.Concat(groupedViewKeys).ToList();

        viewKeys.Should().OnlyHaveUniqueItems();
        viewKeys.Should().HaveCount(21);
    }
}
