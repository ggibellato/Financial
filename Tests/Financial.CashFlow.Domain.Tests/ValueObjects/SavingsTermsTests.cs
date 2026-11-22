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

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(32)]
    public void Create_MonthlyWithoutADayFrom1To31_Throws(int? paymentDay)
    {
        var act = () => SavingsTerms.Create(InterestFrequency.Monthly, paymentDay, isTaxFree: false, [September]);

        act.Should().Throw<ArgumentException>().WithMessage("Interest payment day must be between 1 and 31.");
    }

    [Fact]
    public void Create_DailyWithAPaymentDay_Throws()
    {
        var act = () => SavingsTerms.Create(InterestFrequency.Daily, 5, isTaxFree: false, [September]);

        act.Should().Throw<ArgumentException>().WithMessage("Interest paid daily has no payment day.");
    }

    [Fact]
    public void Create_WithAnUnknownFrequency_Throws()
    {
        var act = () => SavingsTerms.Create((InterestFrequency)99, null, isTaxFree: false, [September]);

        act.Should().Throw<ArgumentException>().WithMessage("Choose how often interest is paid.");
    }

    [Fact]
    public void Create_WithoutRates_Throws()
    {
        var act = () => SavingsTerms.Create(InterestFrequency.Daily, null, isTaxFree: false, []);

        act.Should().Throw<ArgumentException>().WithMessage("A savings account needs at least one rate.");
    }

    [Fact]
    public void Create_WithTwoRatesOnTheSameDate_Throws()
    {
        var act = () => SavingsTerms.Create(
            InterestFrequency.Daily, null, isTaxFree: false, [September, InterestRate.Create(5m, new DateOnly(2026, 9, 1))]);

        act.Should().Throw<ArgumentException>().WithMessage("Two rates can't take effect on the same date.");
    }

    [Theory]
    [InlineData("2026-09-01", 4.5, 3.75)]
    [InlineData("2026-12-31", 4.5, 3.75)]
    [InlineData("2027-01-01", 3.75, null)]
    public void CurrentAndUpcomingRate_AreTheLatestRateInEffectAndTheNextOne(string today, double current, double? upcoming)
    {
        var terms = SavingsTerms.Create(InterestFrequency.Monthly, 1, isTaxFree: false, [September, January]);
        var asOf = DateOnly.Parse(today, System.Globalization.CultureInfo.InvariantCulture);

        using (new AssertionScope())
        {
            terms.CurrentRate(asOf)!.AnnualPercent.Should().Be((decimal)current);
            (terms.UpcomingRate(asOf)?.AnnualPercent).Should().Be((decimal?)upcoming);
        }
    }

    [Fact]
    public void CurrentRate_WhenEveryRateIsInTheFuture_IsNoneAndTheFirstIsUpcoming()
    {
        var terms = SavingsTerms.Create(InterestFrequency.Monthly, 1, isTaxFree: false, [September, January]);

        using (new AssertionScope())
        {
            terms.CurrentRate(new DateOnly(2026, 8, 31)).Should().BeNull();
            terms.UpcomingRate(new DateOnly(2026, 8, 31)).Should().Be(September);
        }
    }
}
