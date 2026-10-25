using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
[Trait("AC", "P60-F02-account-model-and-bank-migration-08")]
public class CapabilityGuardTests
{
    private static readonly DateOnly Date = new(2026, 9, 10);

    private readonly Account _bank = TestAccounts.Bank("Barclays");
    private readonly Category _groceries = Category.Create("Mercado");
    private readonly IncomeSource _salary = IncomeSource.Create("Gleison", IncomeGroup.Salary);

    private static Account Minimal(params Capability[] capabilities) =>
        Account.CreateWithCapabilities("Minimal", Currency.GBP, capabilities.ToHashSet());

    private static Account MinimalWithout(Capability capability) =>
        Minimal(Enum.GetValues<Capability>().Where(c => c != capability && c != Capability.HasStatements).ToArray());

    [Fact]
    public void SetOpeningBalance_AccountWithOnlyHasOpeningBalance_Succeeds()
    {
        var account = Minimal(Capability.HasOpeningBalance);

        account.SetOpeningBalance(25m, Date);

        account.OpeningBalance.Should().Be(OpeningBalance.Create(25m, Date));
    }

    [Fact]
    public void SetOpeningBalance_AccountWithoutHasOpeningBalance_Throws()
    {
        var act = () => MinimalWithout(Capability.HasOpeningBalance).SetOpeningBalance(25m, Date);

        act.Should().Throw<ArgumentException>().WithMessage("Minimal has no opening balance.");
    }

    [Fact]
    public void Income_Create_AccountWithOnlyReceivesIncome_Succeeds()
    {
        var account = Minimal(Capability.ReceivesIncome);

        var income = Income.Create(Date, _salary, null, 100m, account);

        income.Account.Should().BeSameAs(account);
    }

    [Fact]
    public void Income_Create_AccountWithoutReceivesIncome_Throws()
    {
        var act = () => Income.Create(Date, _salary, null, 100m, MinimalWithout(Capability.ReceivesIncome));

        act.Should().Throw<ArgumentException>().WithMessage("Account 'Minimal' cannot receive income.");
    }

    [Fact]
    public void Income_UpdateDetails_AccountWithoutReceivesIncome_ThrowsAndKeepsTheAccount()
    {
        var income = Income.Create(Date, _salary, null, 100m, _bank);

        var act = () => income.UpdateDetails(Date, _salary, null, 100m, MinimalWithout(Capability.ReceivesIncome));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            income.Account.Should().BeSameAs(_bank);
        }
    }

    [Fact]
    public void Expense_Create_PaymentSourceWithOnlyHasCategories_Succeeds()
    {
        var account = Minimal(Capability.HasCategories);

        var expense = Expense.Create(Date, "Groceries", 20m, _groceries, account, creditCard: null);

        expense.PaymentSourceAccount.Should().BeSameAs(account);
    }

    [Fact]
    public void Expense_Create_PaymentSourceWithoutHasCategories_Throws()
    {
        var act = () => Expense.Create(Date, "Groceries", 20m, _groceries, MinimalWithout(Capability.HasCategories), creditCard: null);

        act.Should().Throw<ArgumentException>().WithMessage("Account 'Minimal' cannot be a payment source.");
    }

    [Fact]
    public void Expense_Settle_PaymentSourceWithoutHasCategories_ThrowsAndStaysUnsettled()
    {
        var card = TestAccounts.CreditCard("Amex");
        var expense = Expense.Create(Date, "Groceries", 20m, _groceries, paymentSourceAccount: null, card);

        var act = () => expense.Settle(MinimalWithout(Capability.HasCategories), Date.AddDays(20));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        }
    }

    [Fact]
    public void Transfer_Create_SourceWithOnlyCanSendTransfers_Succeeds()
    {
        var source = Minimal(Capability.CanSendTransfers);

        var transfer = Transfer.Create(Date, source, _bank, 10m, destinationAmount: null, note: null, today: TestClock.Today);

        transfer.SourceAccount.Should().BeSameAs(source);
    }

    [Fact]
    public void Transfer_Create_SourceWithoutCanSendTransfers_Throws()
    {
        var act = () => Transfer.Create(
            Date, MinimalWithout(Capability.CanSendTransfers), _bank, 10m, destinationAmount: null, note: null, today: TestClock.Today);

        act.Should().Throw<ArgumentException>().WithMessage("Transfers from Minimal to Barclays are not allowed.");
    }

    [Fact]
    public void Transfer_Create_DestinationWithOnlyCanReceiveTransfers_Succeeds()
    {
        var destination = Minimal(Capability.CanReceiveTransfers);

        var transfer = Transfer.Create(Date, _bank, destination, 10m, destinationAmount: null, note: null, today: TestClock.Today);

        transfer.DestinationAccount.Should().BeSameAs(destination);
    }

    [Fact]
    public void Transfer_Create_DestinationWithoutCanReceiveTransfers_Throws()
    {
        var act = () => Transfer.Create(
            Date, _bank, MinimalWithout(Capability.CanReceiveTransfers), 10m, destinationAmount: null, note: null, today: TestClock.Today);

        act.Should().Throw<ArgumentException>().WithMessage("Transfers from Barclays to Minimal are not allowed.");
    }

    [Fact]
    public void BalanceAdjustment_Create_AccountWithOnlyCanReconcile_Succeeds()
    {
        var account = Minimal(Capability.CanReconcile);

        var adjustment = BalanceAdjustment.Create(Date, account, 100m, 5m, note: null);

        adjustment.Account.Should().BeSameAs(account);
    }

    [Fact]
    public void BalanceAdjustment_Create_AccountWithoutCanReconcile_Throws()
    {
        var act = () => BalanceAdjustment.Create(Date, MinimalWithout(Capability.CanReconcile), 100m, 5m, note: null);

        act.Should().Throw<ArgumentException>().WithMessage("Account 'Minimal' cannot be reconciled.");
    }

    [Fact]
    public void SetRoundUp_AccountWithOnlySupportsRoundUp_Succeeds()
    {
        var account = Minimal(Capability.SupportsRoundUp);

        account.SetRoundUp(true);

        account.RoundUp.Should().Be(new RoundUpSetting(true));
    }

    [Fact]
    public void Expense_SetRoundUpAmount_PaymentSourceWithSupportsRoundUpEnabled_Succeeds()
    {
        var account = Minimal(Capability.HasCategories, Capability.SupportsRoundUp);
        account.SetRoundUp(true);
        var expense = Expense.Create(Date, "Coffee", 2.30m, _groceries, account, creditCard: null);

        expense.SetRoundUpAmount(0.70m);

        expense.RoundUpAmount.Should().Be(0.70m);
    }

    [Fact]
    public void Expense_SetRoundUpAmount_PaymentSourceWithoutSupportsRoundUp_Throws()
    {
        var expense = Expense.Create(Date, "Coffee", 2.30m, _groceries, MinimalWithout(Capability.SupportsRoundUp), creditCard: null);

        var act = () => expense.SetRoundUpAmount(0.70m);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Bank 'Minimal' does not support round-up.");
            expense.RoundUpAmount.Should().BeNull();
            expense.SuggestedRoundUpAmount.Should().BeNull();
        }
    }

    [Fact]
    public void Expense_SetRoundUpAmount_RoundUpCapableButDisabled_Throws()
    {
        var expense = Expense.Create(Date, "Coffee", 2.30m, _groceries, TestAccounts.Bank("Barclays", roundUpEnabled: false), creditCard: null);

        var act = () => expense.SetRoundUpAmount(0.70m);

        act.Should().Throw<ArgumentException>().WithMessage("Bank 'Barclays' does not support round-up.");
    }

    [Fact]
    public void BankTemplate_AllCapabilities_CombinationWorks()
    {
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: true);
        chase.SetOpeningBalance(100m, Date);

        var expense = Expense.Create(Date, "Coffee", 2.30m, _groceries, chase, creditCard: null);
        expense.SetRoundUpAmount(0.70m);
        var income = Income.Create(Date, _salary, null, 1000m, chase);
        var outgoing = Transfer.Create(Date, chase, _bank, 50m, destinationAmount: null, note: null, today: TestClock.Today);
        var incoming = Transfer.Create(Date, _bank, chase, 20m, destinationAmount: null, note: null, today: TestClock.Today);
        var adjustment = BalanceAdjustment.Create(Date, chase, 1067m, 0m, note: null);

        using (new AssertionScope())
        {
            chase.Has(Capability.CanPayBills).Should().BeTrue();
            expense.ContributionTo(chase).Should().Be(-3.00m);
            income.ContributionTo(chase).Should().Be(1000m);
            outgoing.ContributionTo(chase).Should().Be(-50m);
            incoming.ContributionTo(chase).Should().Be(20m);
            adjustment.ContributionTo(chase).Should().Be(0m);
            chase.OpeningBalance.Should().Be(OpeningBalance.Create(100m, Date));
        }
    }
}
