using Financial.Presentation.App.Navigation;
using FluentAssertions;

namespace Financial.Presentation.Tests.Navigation;

public class NavTreeTests
{
    [Fact]
    public void Categories_HasExactlyTwoCategories()
    {
        NavTree.Categories.Should().HaveCount(2);
        NavTree.Categories[0].Id.Should().Be("investments");
        NavTree.Categories[1].Id.Should().Be("cashflow");
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
    public void AllChildViewKeys_AreUnique()
    {
        var viewKeys = NavTree.Categories.SelectMany(c => c.Children).Select(c => c.ViewKey).ToList();

        viewKeys.Should().OnlyHaveUniqueItems();
        viewKeys.Should().HaveCount(10);
    }
}
