using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class CardDueDateTests
{
    private readonly Account _amex = TestAccounts.CreditCard("BA Amex");
    private readonly Account _visa = TestAccounts.CreditCard("Platinum Visa 8003");

    private static CardStatement Statement(Account card, int year, int month, DateOnly? dueDate = null)
    {
        var statement = CardStatement.Create(card, year, month);
        statement.SetDueDate(dueDate);
        return statement;
    }

    [Fact]
    public void For_SeveralDatedStatements_ReturnsTheLatestByYearThenMonth()
    {
        var statements = new[]
        {
            Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18)),
            Statement(_amex, 2027, 1, new DateOnly(2027, 1, 18)),
            Statement(_amex, 2026, 12, new DateOnly(2026, 12, 18))
        };

        CardDueDate.For(_amex, statements).Should().Be(new DateOnly(2027, 1, 18));
    }

    [Fact]
    public void For_LaterStatementWithoutADate_IsSkipped()
    {
        var statements = new[]
        {
            Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18)),
            Statement(_amex, 2026, 12)
        };

        CardDueDate.For(_amex, statements).Should().Be(new DateOnly(2026, 11, 18));
    }

    [Fact]
    public void For_OtherCardsDatedStatement_IsIgnored()
    {
        var statements = new[]
        {
            Statement(_visa, 2026, 12, new DateOnly(2026, 12, 5)),
            Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18))
        };

        CardDueDate.For(_amex, statements).Should().Be(new DateOnly(2026, 11, 18));
    }

    [Fact]
    public void For_NoStatementWithADate_ReturnsNull()
    {
        var statements = new[] { Statement(_amex, 2026, 11), Statement(_visa, 2026, 11, new DateOnly(2026, 11, 5)) };

        CardDueDate.For(_amex, statements).Should().BeNull();
    }

    [Fact]
    public void ClearSupersededBy_NewDueDate_ClearsOnlyTheCardsLaterDatesSoTheNewOneIsTheLatest()
    {
        var september = Statement(_amex, 2026, 9, new DateOnly(2026, 9, 18));
        var november = Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18));
        var december = Statement(_amex, 2026, 12, new DateOnly(2026, 12, 18));
        var otherCard = Statement(_visa, 2026, 12, new DateOnly(2026, 12, 5));

        CardDueDate.ClearSupersededBy(_amex, [september, november, december, otherCard], new DateOnly(2026, 11, 20));

        new[] { september.DueDate, november.DueDate, december.DueDate, otherCard.DueDate }.Should().Equal(
            new DateOnly(2026, 9, 18), new DateOnly(2026, 11, 18), null, new DateOnly(2026, 12, 5));
    }

    [Fact]
    public void ClearSupersededBy_NoDueDate_ClearsEveryDateOfTheCard()
    {
        var september = Statement(_amex, 2026, 9, new DateOnly(2026, 9, 18));
        var november = Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18));
        var otherCard = Statement(_visa, 2026, 11, new DateOnly(2026, 11, 5));

        CardDueDate.ClearSupersededBy(_amex, [september, november, otherCard], null);

        new[] { september.DueDate, november.DueDate, otherCard.DueDate }.Should().Equal(null, null, new DateOnly(2026, 11, 5));
    }
}
