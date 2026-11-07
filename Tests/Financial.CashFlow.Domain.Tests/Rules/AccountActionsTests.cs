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
    public void AllowedFor_Bank_ReturnsEveryActionInOrder()
    {
        AccountActions.AllowedFor(TestAccounts.Bank("Barclays")).Should().Equal(
            AccountAction.AddExpense,
            AccountAction.AddIncome,
            AccountAction.TransferTo,
            AccountAction.TransferFrom,
            AccountAction.PayRecurringBill,
            AccountAction.PayCardStatement,
            AccountAction.ReceiveCardCredit,
            AccountAction.Reconcile,
            AccountAction.WithdrawFromPots);
    }

    [Theory]
    [InlineData(Capability.HasCategories, AccountAction.AddExpense)]
    [InlineData(Capability.ReceivesIncome, AccountAction.AddIncome)]
    [InlineData(Capability.CanSendTransfers, AccountAction.TransferTo)]
    [InlineData(Capability.CanReconcile, AccountAction.Reconcile)]
    public void AllowedFor_SingleCapability_ReturnsOnlyItsAction(Capability capability, AccountAction expected)
    {
        AccountActions.AllowedFor(Minimal(capability)).Should().Equal(expected);
    }

    [Fact]
    public void AllowedFor_CanReceiveTransfers_ReturnsTransferFromAndReceiveCardCredit()
    {
        AccountActions.AllowedFor(Minimal(Capability.CanReceiveTransfers)).Should().Equal(AccountAction.TransferFrom, AccountAction.ReceiveCardCredit);
    }

    [Fact]
    public void AllowedFor_HasStatements_ReturnsAddChargeAndAddRefund()
    {
        AccountActions.AllowedFor(Minimal(Capability.HasStatements)).Should().Equal(AccountAction.AddCharge, AccountAction.AddRefund);
    }

    [Fact]
    public void AllowedFor_CanPayBills_ReturnsThePaymentsAndWithdrawFromPots()
    {
        AccountActions.AllowedFor(Minimal(Capability.CanPayBills)).Should().Equal(
            AccountAction.PayRecurringBill, AccountAction.PayCardStatement, AccountAction.WithdrawFromPots);
    }

    [Fact]
    public void AllowedFor_HasPots_ReturnsDepositAndManagePots()
    {
        AccountActions.AllowedFor(Minimal(Capability.HasPots)).Should().Equal(AccountAction.Deposit, AccountAction.ManagePots);
    }

    [Fact]
    public void AllowedFor_CanWithdraw_ReturnsWithdraw()
    {
        AccountActions.AllowedFor(Minimal(Capability.CanWithdraw)).Should().Equal(AccountAction.Withdraw);
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
    public void AllowedFor_CreditCardAccount_ReturnsOnlyAddChargeAndAddRefund()
    {
        AccountActions.AllowedFor(TestAccounts.CreditCard("BA Amex")).Should().Equal(AccountAction.AddCharge, AccountAction.AddRefund);
    }

    [Fact]
    public void AllowedFor_PotsAccount_ReturnsDepositWithdrawAndManagePots()
    {
        var pots = Account.Create("Holiday pots", AccountTemplate.Pots, Currency.GBP);

        AccountActions.AllowedFor(pots).Should().Equal(AccountAction.Deposit, AccountAction.Withdraw, AccountAction.ManagePots);
    }

    [Fact]
    public void AllowedFor_InactivePotsAccount_ReturnsNone()
    {
        AccountActions.AllowedFor(TestAccounts.Pots("Holiday pots", isActive: false)).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_InactiveCreditCardAccount_ReturnsNone()
    {
        var inactive = Account.CreateWithCapabilities(
            "Closed card", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.CreditCard), isActive: false);

        AccountActions.AllowedFor(inactive).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_CreditCardInCredit_AddsRefundCreditToBankAfterAddRefund()
    {
        AccountActions.AllowedFor(TestAccounts.CreditCard("BA Amex"), isInCredit: true)
            .Should().Equal(AccountAction.AddCharge, AccountAction.AddRefund, AccountAction.RefundCreditToBank);
    }

    [Fact]
    public void AllowedFor_InactiveCreditCardInCredit_ReturnsNone()
    {
        var inactive = Account.CreateWithCapabilities(
            "Closed card", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.CreditCard), isActive: false);

        AccountActions.AllowedFor(inactive, isInCredit: true).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_BankFlaggedInCredit_DoesNotOfferRefundCreditToBank()
    {
        AccountActions.AllowedFor(TestAccounts.Bank("Barclays"), isInCredit: true).Should().NotContain(AccountAction.RefundCreditToBank);
    }

    [Fact]
    public void AllowedFor_AccountWithCategoriesAndStatements_HasAddChargeInsteadOfAddExpense()
    {
        AccountActions.AllowedFor(Minimal(Capability.HasCategories, Capability.HasStatements, Capability.ReceivesIncome))
            .Should().Equal(AccountAction.AddCharge, AccountAction.AddRefund, AccountAction.AddIncome);
    }

    [Fact]
    public void AllowedFor_SettlementAccount_ReturnsAddEntryAndSettle()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");

        AccountActions.AllowedFor(mae).Should().Equal(AccountAction.AddEntry, AccountAction.Settle);
    }

    [Fact]
    public void AllowedFor_InactiveSettlementAccount_ReturnsNone()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");
        mae.SetActive(false);

        AccountActions.AllowedFor(mae).Should().BeEmpty();
    }

    [Fact]
    public void AllowedFor_HasParticipantsWithoutCanSettle_ReturnsOnlyAddEntry()
    {
        AccountActions.AllowedFor(Minimal(Capability.HasParticipants)).Should().Equal(AccountAction.AddEntry);
    }

    [Fact]
    public void DisabledFor_SettlementAccountWhoseEveryBalanceRoundsToZero_DisablesSettleWithNothingToSettle()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");

        AccountActions.DisabledFor(mae, [(Currency.BRL, -0.0032m), (Currency.GBP, 0.00214099m)])
            .Should().Equal(new Dictionary<AccountAction, string> { [AccountAction.Settle] = "Nothing to settle." });
    }

    [Fact]
    public void DisabledFor_SettlementAccountWithOneCurrencyOutstanding_DisablesNothing()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");

        AccountActions.DisabledFor(mae, [(Currency.BRL, 0m), (Currency.GBP, 0.005m)]).Should().BeEmpty();
    }

    [Fact]
    public void DisabledFor_InactiveSettledAccount_DisablesNothing()
    {
        var mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");
        mae.SetActive(false);

        AccountActions.DisabledFor(mae, [(Currency.BRL, 0m), (Currency.GBP, 0m)]).Should().BeEmpty();
    }

    [Fact]
    public void DisabledFor_Bank_DisablesNothing()
    {
        AccountActions.DisabledFor(TestAccounts.Bank("Barclays"), []).Should().BeEmpty();
    }

    [Fact]
    public void DisabledFor_NullBalances_Throws()
    {
        var act = () => AccountActions.DisabledFor(TestAccounts.Bank("Barclays"), null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
