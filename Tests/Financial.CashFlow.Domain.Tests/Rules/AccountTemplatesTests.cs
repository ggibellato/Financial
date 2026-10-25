using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class AccountTemplatesTests
{
    [Fact]
    public void CapabilitiesOf_Bank_IsExactlyTheEightBankCapabilities()
    {
        AccountTemplates.CapabilitiesOf(AccountTemplate.Bank).Should().BeEquivalentTo(new[]
        {
            Capability.HasOpeningBalance,
            Capability.ReceivesIncome,
            Capability.HasCategories,
            Capability.CanSendTransfers,
            Capability.CanReceiveTransfers,
            Capability.CanReconcile,
            Capability.CanPayBills,
            Capability.SupportsRoundUp
        });
    }

    [Fact]
    public void CapabilitiesOf_CreditCard_IsExactlyStatementsCategoriesAndPayments()
    {
        AccountTemplates.CapabilitiesOf(AccountTemplate.CreditCard).Should().BeEquivalentTo(new[]
        {
            Capability.HasStatements,
            Capability.HasCategories,
            Capability.ReceivesPayments
        });
    }

    [Fact]
    public void JoinsDefaultGroup_CreditCard_IsTrue()
    {
        AccountTemplates.JoinsDefaultGroup(AccountTemplate.CreditCard).Should().BeTrue();
    }

    [Fact]
    public void AllowedCurrencies_CreditCard_IsGbpOnly()
    {
        AccountTemplates.AllowedCurrencies(AccountTemplate.CreditCard).Should().Equal(Currency.GBP);
    }

    [Fact]
    public void CapabilitiesOf_UndefinedTemplate_Throws()
    {
        var act = () => AccountTemplates.CapabilitiesOf((AccountTemplate)99);

        act.Should().Throw<ArgumentException>().WithMessage("Account template '99' is not defined.");
    }

    [Fact]
    public void JoinsDefaultGroup_Bank_IsTrue()
    {
        AccountTemplates.JoinsDefaultGroup(AccountTemplate.Bank).Should().BeTrue();
    }

    [Fact]
    public void JoinsDefaultGroup_UndefinedTemplate_IsFalse()
    {
        AccountTemplates.JoinsDefaultGroup((AccountTemplate)99).Should().BeFalse();
    }

    [Fact]
    public void AllowedCurrencies_Bank_IsGbpOnly()
    {
        AccountTemplates.AllowedCurrencies(AccountTemplate.Bank).Should().Equal(Currency.GBP);
    }

    [Fact]
    public void AllowedCurrencies_UndefinedTemplate_Throws()
    {
        var act = () => AccountTemplates.AllowedCurrencies((AccountTemplate)99);

        act.Should().Throw<ArgumentException>().WithMessage("Account template '99' is not defined.");
    }

    [Fact]
    public void Available_ListsBankAndCreditCard()
    {
        AccountTemplates.Available.Should().Equal(AccountTemplate.Bank, AccountTemplate.CreditCard);
    }

    public static TheoryData<AccountTemplate> AvailableTemplates => new(AccountTemplates.Available);

    [Theory]
    [MemberData(nameof(AvailableTemplates))]
    public void Available_EveryTemplate_HasCapabilitiesAndAtLeastOneCurrency(AccountTemplate template)
    {
        using (new AssertionScope())
        {
            AccountTemplates.CapabilitiesOf(template).Should().NotBeEmpty();
            AccountTemplates.AllowedCurrencies(template).Should().NotBeEmpty();
        }
    }
}
