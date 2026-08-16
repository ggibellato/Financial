using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.CreditCards;

public class CreditCardMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllFiveCardsActiveWithNoDueDate()
    {
        var data = CashFlowData.Create();

        var summary = CreditCardMigrator.Migrate(data);

        summary.CardsSeededCount.Should().Be(5);
        summary.CardsAlreadyPresentCount.Should().Be(0);
        data.CreditCards.Should().ContainSingle(c => c.Name == "Platinum Visa 8003" && c.IsActive && c.NextInvoiceDueDate == null);
        data.CreditCards.Should().ContainSingle(c => c.Name == "Platinum Visa 6007" && c.IsActive && c.NextInvoiceDueDate == null);
        data.CreditCards.Should().ContainSingle(c => c.Name == "Chase Master 4023" && c.IsActive && c.NextInvoiceDueDate == null);
        data.CreditCards.Should().ContainSingle(c => c.Name == "BA Amex" && c.IsActive && c.NextInvoiceDueDate == null);
        data.CreditCards.Should().ContainSingle(c => c.Name == "Paypal Credit" && c.IsActive && c.NextInvoiceDueDate == null);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRunAndKeepsSameIds()
    {
        var data = CashFlowData.Create();
        CreditCardMigrator.Migrate(data);
        var idsAfterFirstRun = data.CreditCards.Select(c => c.Id).OrderBy(id => id).ToList();

        var secondSummary = CreditCardMigrator.Migrate(data);

        secondSummary.CardsSeededCount.Should().Be(0);
        secondSummary.CardsAlreadyPresentCount.Should().Be(5);
        data.CreditCards.Should().HaveCount(5);
        data.CreditCards.Select(c => c.Id).OrderBy(id => id).Should().Equal(idsAfterFirstRun);
    }

    [Fact]
    public void Migrate_WithSomeCardsAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddCreditCard(CreditCard.Create("BA Amex"));

        var summary = CreditCardMigrator.Migrate(data);

        summary.CardsSeededCount.Should().Be(4);
        summary.CardsAlreadyPresentCount.Should().Be(1);
        data.CreditCards.Should().HaveCount(5);
    }

    [Fact]
    public void Migrate_WithNullData_Throws()
    {
        var act = () => CreditCardMigrator.Migrate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
