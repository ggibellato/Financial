using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
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
}
