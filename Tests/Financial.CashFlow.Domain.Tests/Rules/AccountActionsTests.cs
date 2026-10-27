using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class AccountActionsTests
{
    private static Account Minimal(params Capability[] capabilities) =>
        Account.CreateWithCapabilities("Minimal", Currency.GBP, capabilities.ToHashSet());

    [Fact]
    public void AllowedFor_Bank_ReturnsAllSixActionsInOrder()
    {
        AccountActions.AllowedFor(TestAccounts.Bank("Barclays")).Should().Equal(
            AccountAction.AddExpense,
            AccountAction.AddIncome,
            AccountAction.TransferTo,
            AccountAction.TransferFrom,
            AccountAction.PayRecurringBill,
            AccountAction.Reconcile);
    }

    [Theory]
    [InlineData(Capability.HasCategories, AccountAction.AddExpense)]
    [InlineData(Capability.HasStatements, AccountAction.AddCharge)]
    [InlineData(Capability.ReceivesIncome, AccountAction.AddIncome)]
    [InlineData(Capability.CanSendTransfers, AccountAction.TransferTo)]
    [InlineData(Capability.CanReceiveTransfers, AccountAction.TransferFrom)]
    [InlineData(Capability.CanPayBills, AccountAction.PayRecurringBill)]
    [InlineData(Capability.CanReconcile, AccountAction.Reconcile)]
    public void AllowedFor_SingleCapability_ReturnsOnlyItsAction(Capability capability, AccountAction expected)
    {
        AccountActions.AllowedFor(Minimal(capability)).Should().Equal(expected);
    }

    [Fact]
    public void AllowedFor_CapabilitiesWithoutAnAction_ReturnsNone()
    {
        AccountActions.AllowedFor(Minimal(Capability.HasOpeningBalance, Capability.SupportsRoundUp)).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_InactiveBank_ReturnsNone()
    {
        var inactive = Account.CreateWithCapabilities(
            "Closed", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false);

        AccountActions.AllowedFor(inactive).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_NullAccount_Throws()
    {
        var act = () => AccountActions.AllowedFor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AllowedFor_CreditCardAccount_ReturnsOnlyAddCharge()
    {
        AccountActions.AllowedFor(TestAccounts.CreditCard("BA Amex")).Should().Equal(AccountAction.AddCharge);
    }

    [Fact]
    public void AllowedFor_InactiveCreditCardAccount_ReturnsNone()
    {
        var inactive = Account.CreateWithCapabilities(
            "Closed card", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.CreditCard), isActive: false);

        AccountActions.AllowedFor(inactive).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_AccountWithCategoriesAndStatements_HasAddChargeInsteadOfAddExpense()
    {
        AccountActions.AllowedFor(Minimal(Capability.HasCategories, Capability.HasStatements, Capability.ReceivesIncome))
            .Should().Equal(AccountAction.AddCharge, AccountAction.AddIncome);
    }
}
