using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class SavingsTermsTests
{
    private static readonly InterestRate September = InterestRate.Create(4.5m, new DateOnly(2026, 9, 1));
    private static readonly InterestRate January = InterestRate.Create(3.75m, new DateOnly(2027, 1, 1));

    [Fact]
    public void Create_Monthly_KeepsTheTermsWithRatesOrderedByDate()
    {
        var terms = SavingsTerms.Create(InterestFrequency.Monthly, 5, isTaxFree: true, [January, September]);

        using (new AssertionScope())
        {
            terms.Frequency.Should().Be(InterestFrequency.Monthly);
            terms.PaymentDay.Should().Be(5);
            terms.IsTaxFree.Should().BeTrue();
            terms.Rates.Should().Equal(September, January);
        }
    }

    [Fact]
    public void Create_DailyWithoutAPaymentDay_Succeeds()
    {
        var terms = SavingsTerms.Create(InterestFrequency.Daily, null, isTaxFree: false, [September]);

        terms.PaymentDay.Should().BeNull();
    }

    public static TheoryData<InterestFrequency, int?, InterestRate[], string> InvalidTerms => new()
    {
        { InterestFrequency.Monthly, null, [September], "Interest payment day must be between 1 and 31." },
        { InterestFrequency.Monthly, 0, [September], "Interest payment day must be between 1 and 31." },
        { InterestFrequency.Monthly, 32, [September], "Interest payment day must be between 1 and 31." },
        { InterestFrequency.Daily, 5, [September], "Interest paid daily has no payment day." },
        { (InterestFrequency)99, null, [September], SavingsTerms.UnknownFrequencyMessage },
        { InterestFrequency.Daily, null, [], SavingsTerms.NoRatesMessage },
        { InterestFrequency.Daily, null, [September, InterestRate.Create(5m, new DateOnly(2026, 9, 1))], "Two rates can't take effect on the same date." }
    };

    [Theory]
    [Trait("AC", "P64-F01-savings-template-04")]
    [Trait("AC", "P64-F01-savings-template-05")]
    [MemberData(nameof(InvalidTerms))]
    public void Create_WithInvalidTerms_ThrowsTheRuleMessage(InterestFrequency frequency, int? paymentDay, InterestRate[] rates, string message)
    {
        var act = () => SavingsTerms.Create(frequency, paymentDay, isTaxFree: false, rates);

        act.Should().Throw<ArgumentException>().WithMessage(message);
    }

    [Theory]
    [InlineData("2026-08-31", null, 4.5)]
    [InlineData("2026-09-01", 4.5, 3.75)]
    [InlineData("2026-12-31", 4.5, 3.75)]
    [InlineData("2027-01-01", 3.75, null)]
    public void CurrentAndUpcomingRate_AreTheLatestRateInEffectAndTheNextOne(string today, double? current, double? upcoming)
    {
        var terms = SavingsTerms.Create(InterestFrequency.Monthly, 1, isTaxFree: false, [September, January]);
        var asOf = DateOnly.Parse(today, System.Globalization.CultureInfo.InvariantCulture);

        using (new AssertionScope())
        {
            (terms.CurrentRate(asOf)?.AnnualPercent).Should().Be((decimal?)current);
            (terms.UpcomingRate(asOf)?.AnnualPercent).Should().Be((decimal?)upcoming);
        }
    }
}
