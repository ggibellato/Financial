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

    private static CardStatementLedgerMonth LedgerMonth(CardStatementLedgerStatus status) =>
        new(2026, 10, null, 0m, 0m, 0m, 0m, null, status);

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
    public void SetOn_DateInTheStatementsMonth_SetsItClearsTheCardsLaterDatesAndLeavesEarlierOnes()
    {
        var september = Statement(_amex, 2026, 9, new DateOnly(2026, 9, 18));
        var october = Statement(_amex, 2026, 10, new DateOnly(2026, 10, 18));
        var november = Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18));
        var otherCard = Statement(_visa, 2026, 11, new DateOnly(2026, 11, 5));

        CardDueDate.SetOn(_amex, [september, october, november, otherCard], october, new DateOnly(2026, 10, 25));

        new[] { september.DueDate, october.DueDate, november.DueDate, otherCard.DueDate }.Should().Equal(
            new DateOnly(2026, 9, 18), new DateOnly(2026, 10, 25), null, new DateOnly(2026, 11, 5));
    }

    [Fact]
    public void SetOn_LaterStatementIsPaid_ClearsItsDateToo()
    {
        var october = Statement(_amex, 2026, 10);
        var november = Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18));
        november.MarkPaidBy(Guid.NewGuid());

        CardDueDate.SetOn(_amex, [october, november], october, new DateOnly(2026, 10, 25));

        november.DueDate.Should().BeNull();
    }

    [Theory]
    [InlineData(2026, 11, 1)]
    [InlineData(2026, 9, 30)]
    [InlineData(2025, 10, 25)]
    public void SetOn_DateOutsideTheStatementsMonth_IsRefusedAndChangesNothing(int year, int month, int day)
    {
        var october = Statement(_amex, 2026, 10, new DateOnly(2026, 10, 18));
        var november = Statement(_amex, 2026, 11, new DateOnly(2026, 11, 18));

        var act = () => CardDueDate.SetOn(_amex, [october, november], october, new DateOnly(year, month, day));

        act.Should().Throw<ArgumentException>().WithMessage("The due date must be in October 2026.");
        new[] { october.DueDate, november.DueDate }.Should().Equal(new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 18));
    }

    [Theory]
    [InlineData(CardStatementLedgerStatus.Unpaid)]
    [InlineData(CardStatementLedgerStatus.NothingDue)]
    public void EnsureDueDateAllowed_RowThatIsNotPaid_IsAccepted(CardStatementLedgerStatus status)
    {
        var act = () => CardDueDate.EnsureDueDateAllowed(LedgerMonth(status), new DateOnly(2026, 10, 25));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureDueDateAllowed_PaidRow_IsRefused()
    {
        var act = () => CardDueDate.EnsureDueDateAllowed(LedgerMonth(CardStatementLedgerStatus.Paid), null);

        act.Should().Throw<ArgumentException>().WithMessage("The October 2026 statement is paid, so its due date can't change.");
    }

    [Fact]
    public void EnsureDueDateAllowed_DateOutsideTheMonth_IsRefused()
    {
        var act = () => CardDueDate.EnsureDueDateAllowed(LedgerMonth(CardStatementLedgerStatus.Unpaid), new DateOnly(2026, 11, 1));

        act.Should().Throw<ArgumentException>().WithMessage("The due date must be in October 2026.");
    }
}
