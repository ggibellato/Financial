using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;
using FluentAssertions;
using Financial.TestUtilities;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.CreditCards;

[Trait("Category", "Unit")]
public class CreditCardMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_SeedsAllFiveCardsActiveWithNoDueDate()
    {
        var data = CashFlowData.Create();

        var summary = CreditCardMigrator.Migrate(data);

        summary.CardsSeededCount.Should().Be(5);
        summary.CardsAlreadyPresentCount.Should().Be(0);
        data.Accounts.Should().ContainSingle(c => c.Name == "Platinum Visa 8003" && c.IsActive);
        data.Accounts.Should().ContainSingle(c => c.Name == "Platinum Visa 6007" && c.IsActive);
        data.Accounts.Should().ContainSingle(c => c.Name == "Chase Master 4023" && c.IsActive);
        data.Accounts.Should().ContainSingle(c => c.Name == "BA Amex" && c.IsActive);
        data.Accounts.Should().ContainSingle(c => c.Name == "Paypal Credit" && c.IsActive);
    }

    [Fact]
    public void Migrate_CalledTwice_SeedsNothingNewOnSecondRunAndKeepsSameIds()
    {
        var data = CashFlowData.Create();
        CreditCardMigrator.Migrate(data);
        var idsAfterFirstRun = data.Accounts.Select(c => c.Id).OrderBy(id => id).ToList();

        var secondSummary = CreditCardMigrator.Migrate(data);

        secondSummary.CardsSeededCount.Should().Be(0);
        secondSummary.CardsAlreadyPresentCount.Should().Be(5);
        data.Accounts.Should().HaveCount(5);
        data.Accounts.Select(c => c.Id).OrderBy(id => id).Should().Equal(idsAfterFirstRun);
    }

    [Fact]
    public void Migrate_WithSomeCardsAlreadySeeded_OnlySeedsTheMissingOnes()
    {
        var data = CashFlowData.Create();
        data.AddAccount(TestAccounts.CreditCard("BA Amex"));

        var summary = CreditCardMigrator.Migrate(data);

        summary.CardsSeededCount.Should().Be(4);
        summary.CardsAlreadyPresentCount.Should().Be(1);
        data.Accounts.Should().HaveCount(5);
    }
}
