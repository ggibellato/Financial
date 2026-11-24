using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class ExternalPaymentTests
{
    private static readonly DateOnly Date = new(2026, 11, 12);

    private static Account RoundUpSaver() =>
        TestAccounts.Savings(
            "Round-up saver",
            SavingsTerms.Create(InterestFrequency.Monthly, 1, isTaxFree: false, [InterestRate.Create(5m, new DateOnly(2026, 1, 1))], roundUpOnly: true));

    [Theory]
    [Trait("AC", "P67-F01-pay-in-out-01")]
    [InlineData(PaymentDirection.In, 12.50)]
    [InlineData(PaymentDirection.Out, -12.50)]
    public void ContributionTo_TheAccount_IsSignedByDirection(PaymentDirection direction, decimal expected)
    {
        var chaseSave = TestAccounts.Savings("Chase Save");

        var payment = ExternalPayment.Create(direction, Date, chaseSave, 12.50m, "  Chase cashback  ");

        using (new AssertionScope())
        {
            payment.ContributionTo(chaseSave).Should().Be(expected);
            payment.ContributionTo(TestAccounts.Bank("Barclays")).Should().Be(0m);
            payment.Description.Should().Be("Chase cashback");
        }
    }

    [Theory]
    [Trait("AC", "P67-F01-pay-in-out-02")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.005)]
    public void Create_WithAnInvalidAmount_Throws(decimal amount)
    {
        var act = () => ExternalPayment.Create(PaymentDirection.In, Date, TestAccounts.Bank("Barclays"), amount, "Gift");

        act.Should().Throw<ArgumentException>().WithMessage(ExternalPayment.AmountMessage);
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-02")]
    public void Create_WithoutADescription_Throws()
    {
        var act = () => ExternalPayment.Create(PaymentDirection.Out, Date, TestAccounts.Bank("Barclays"), 5m, "  ");

        act.Should().Throw<ArgumentException>().WithMessage(ExternalPayment.DescriptionMessage);
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-03")]
    public void Create_PayInToARoundUpOnlyAccount_ThrowsButAPayOutIsAllowed()
    {
        var saver = RoundUpSaver();

        var payIn = () => ExternalPayment.Create(PaymentDirection.In, Date, saver, 5m, "Gift");
        var payOut = ExternalPayment.Create(PaymentDirection.Out, Date, saver, 5m, "Moved out");

        using (new AssertionScope())
        {
            payIn.Should().Throw<ArgumentException>().WithMessage("Round-up saver only receives round-ups.");
            payOut.ContributionTo(saver).Should().Be(-5m);
        }
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-04")]
    public void Create_OnACreditCard_Throws()
    {
        var act = () => ExternalPayment.Create(PaymentDirection.In, Date, TestAccounts.CreditCard("Amex"), 5m, "Gift");

        act.Should().Throw<ArgumentException>().WithMessage("Amex can't take a pay-in or pay-out.");
    }

    [Fact]
    public void EnsureCanSwitchToRoundUpOnly_WithAPayIn_Throws()
    {
        var chaseSave = TestAccounts.Savings("Chase Save");
        var payIn = ExternalPayment.Create(PaymentDirection.In, Date, chaseSave, 5m, "Cashback");
        var roundUpOnly = SavingsTerms.Create(InterestFrequency.Daily, null, isTaxFree: false, [InterestRate.Create(1m, new DateOnly(2026, 1, 1))], roundUpOnly: true);

        var act = () => chaseSave.EnsureCanSwitchToRoundUpOnly(roundUpOnly, [payIn]);

        act.Should().Throw<ArgumentException>().WithMessage("Chase Save already has money in that isn't a round-up or interest.");
    }
}
