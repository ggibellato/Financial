using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class RoundUpDestinationTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private static readonly Category Mercado = Category.Create("Mercado");

    private static readonly SavingsTerms RoundUpOnlyTerms =
        SavingsTerms.Create(InterestFrequency.Monthly, 1, isTaxFree: false, [InterestRate.Create(5m, new DateOnly(2026, 1, 1))], roundUpOnly: true);

    private static Account RoundUpSaver(string name = "Chase round-up saver") => TestAccounts.Savings(name, RoundUpOnlyTerms);

    private static Account ChaseWithRoundUpsTo(Account saver)
    {
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: true, cashbackRatePercent: 2m);
        chase.SetRoundUpDestination(MoneyDestination.ToAccount(saver.Id), saver);
        return chase;
    }

    private static Expense PaidFrom(Account bank, decimal roundUp, decimal? cashback = null)
    {
        var expense = Expense.Create(new DateOnly(2026, 11, 3), "Lunch", 12.40m, Mercado, bank, null);
        expense.SetRoundUpAmount(roundUp);
        expense.SetCashbackAmount(cashback);
        return expense;
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-02")]
    public void Transfer_IntoARoundUpOnlyAccount_Throws()
    {
        var saver = RoundUpSaver();

        var act = () => Transfer.Create(Today, TestAccounts.Bank("Barclays"), saver, 50m, null, null, Today);

        act.Should().Throw<ArgumentException>().WithMessage("Chase round-up saver only receives round-ups.");
    }

    [Fact]
    public void Transfer_OutOfARoundUpOnlyAccount_IsAllowed()
    {
        var transfer = Transfer.Create(Today, RoundUpSaver(), TestAccounts.Savings("Chase Save"), 50m, null, null, Today);

        transfer.Amount.Should().Be(50m);
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-03")]
    public void Income_IntoARoundUpOnlyAccount_AllowsOnlyInterest()
    {
        var saver = RoundUpSaver();

        var bonus = () => Income.Create(Today, IncomeSource.Create("Bonus", IncomeGroup.NonReportable), null, 10m, saver);
        var interest = Income.Create(Today, IncomeSource.Create("Interest", IncomeGroup.DividendoJuros), null, 1.20m, saver);

        using (new AssertionScope())
        {
            bonus.Should().Throw<ArgumentException>().WithMessage("Chase round-up saver only receives round-ups and its interest.");
            interest.IsInterest.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-04")]
    [Trait("AC", "P67-F01-pay-in-out-06")]
    public void AllowedFor_ARoundUpOnlyAccount_OffersNoTransferFromOrAddIncome()
    {
        AccountActions.AllowedFor(RoundUpSaver()).Should().Equal(
            AccountAction.AddInterest, AccountAction.TransferTo, AccountAction.Reconcile, AccountAction.PayOut);
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-05")]
    public void EnsureCanSwitchToRoundUpOnly_WithATransferIn_Throws()
    {
        var saver = TestAccounts.Savings("Chase round-up saver");
        var transferIn = Transfer.Create(Today, TestAccounts.Bank("Barclays"), saver, 50m, null, null, Today);

        var act = () => saver.EnsureCanSwitchToRoundUpOnly(RoundUpOnlyTerms, [transferIn], [], []);

        act.Should().Throw<ArgumentException>().WithMessage("Chase round-up saver already has money in that isn't a round-up or interest.");
    }

    [Fact]
    public void EnsureCanSwitchToRoundUpOnly_WhenNotSwitchingOn_IgnoresTransfersIn()
    {
        var saver = TestAccounts.Savings("Chase Save");
        var transferIn = Transfer.Create(Today, TestAccounts.Bank("Barclays"), saver, 50m, null, null, Today);

        var act = () => saver.EnsureCanSwitchToRoundUpOnly(saver.Savings, [transferIn], [], []);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanSwitchToRoundUpOnly_WithOnlyInterest_Passes()
    {
        var saver = TestAccounts.Savings("Chase round-up saver");
        var interest = Income.Create(Today, IncomeSource.Create("Interest", IncomeGroup.DividendoJuros), null, 1.20m, saver);

        var act = () => saver.EnsureCanSwitchToRoundUpOnly(RoundUpOnlyTerms, [], [interest], []);

        act.Should().NotThrow();
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-01")]
    public void Bank_WithoutDestinations_HasNone()
    {
        var bank = TestAccounts.Bank("Barclays");

        using (new AssertionScope())
        {
            bank.RoundUpDestination.Should().Be(MoneyDestination.None);
            bank.CashbackDestination.Should().Be(MoneyDestination.None);
            TestAccounts.Savings("Chase Save").RoundUpDestination.Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-02")]
    public void SetRoundUpDestination_ToASavingsAccountThatIsNotRoundUpOnly_Throws()
    {
        var chaseSave = TestAccounts.Savings("Chase Save");
        var bank = TestAccounts.Bank("Chase", roundUpEnabled: true);

        var act = () => bank.SetRoundUpDestination(MoneyDestination.ToAccount(chaseSave.Id), chaseSave);

        act.Should().Throw<ArgumentException>().WithMessage(Account.RoundUpDestinationMessage);
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-02")]
    public void SetCashbackDestination_ToARoundUpOnlyAccount_Throws()
    {
        var saver = RoundUpSaver();
        var bank = TestAccounts.Bank("Chase", cashbackRatePercent: 2m);

        var act = () => bank.SetCashbackDestination(MoneyDestination.ToAccount(saver.Id), saver);

        act.Should().Throw<ArgumentException>().WithMessage(Account.CashbackDestinationMessage);
    }

    [Fact]
    public void SetCashbackDestination_InvestedAutomatically_NeedsNoAccount()
    {
        var bank = TestAccounts.Bank("Trading 212", cashbackRatePercent: 1.5m);

        bank.SetCashbackDestination(MoneyDestination.InvestedAutomatically);

        bank.CashbackDestination.Should().Be(MoneyDestination.InvestedAutomatically);
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-03")]
    public void ContributionTo_TheRoundUpDestination_IsTheRoundUpWhileTheBankStillPaysIt()
    {
        var saver = RoundUpSaver();
        var chase = ChaseWithRoundUpsTo(saver);
        var expense = PaidFrom(chase, 0.60m, cashback: 0.24m);

        using (new AssertionScope())
        {
            expense.ContributionTo(saver).Should().Be(0.60m);
            expense.ContributionTo(chase).Should().Be(-13.00m);
        }
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-03")]
    public void ContributionTo_TheCashbackDestination_IsTheCashback()
    {
        var cashbackSaver = TestAccounts.Savings("Chase cashback");
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: true, cashbackRatePercent: 2m);
        chase.SetCashbackDestination(MoneyDestination.ToAccount(cashbackSaver.Id), cashbackSaver);

        PaidFrom(chase, 0.60m, cashback: 0.24m).ContributionTo(cashbackSaver).Should().Be(0.24m);
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-05")]
    public void ContributionTo_AfterTheDestinationIsSetToNone_IsZero()
    {
        var saver = RoundUpSaver();
        var chase = ChaseWithRoundUpsTo(saver);
        var expense = PaidFrom(chase, 0.60m);

        chase.SetRoundUpDestination(MoneyDestination.None);

        expense.ContributionTo(saver).Should().Be(0m);
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-03")]
    public void Balance_OfTheDestination_IgnoresRoundUpsBeforeItsStartDate()
    {
        var saver = RoundUpSaver();
        saver.SetOpeningBalance(10m, new DateOnly(2026, 11, 2));
        var chase = ChaseWithRoundUpsTo(saver);
        var before = Expense.Create(new DateOnly(2026, 11, 1), "Coffee", 2.30m, Mercado, chase, null);
        before.SetRoundUpAmount(0.70m);

        var balance = AccountBalanceRule.Balance(saver, Today, [before, PaidFrom(chase, 0.60m)]);

        balance.Should().Be(10.60m);
    }
}
