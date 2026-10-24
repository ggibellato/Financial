using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

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
    public void Available_ListsBank()
    {
        AccountTemplates.Available.Should().Equal(AccountTemplate.Bank);
    }
}
