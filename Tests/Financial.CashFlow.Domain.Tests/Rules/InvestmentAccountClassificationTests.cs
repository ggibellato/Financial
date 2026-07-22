using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class InvestmentAccountClassificationTests
{
    [Theory]
    [InlineData(InvestmentAccount.PlatinumVisa8003)]
    [InlineData(InvestmentAccount.PlatinumVisa6007)]
    [InlineData(InvestmentAccount.ChaseMaster4023)]
    [InlineData(InvestmentAccount.BaAmex)]
    [InlineData(InvestmentAccount.PaypalCredit)]
    public void IsLiability_ForLiabilityAccounts_ReturnsTrue(InvestmentAccount account)
    {
        InvestmentAccountClassification.IsLiability(account).Should().BeTrue();
    }

    [Theory]
    [InlineData(InvestmentAccount.BlueRewardsSaver)]
    [InlineData(InvestmentAccount.ChipCashIsaGleison)]
    [InlineData(InvestmentAccount.ChaseSave)]
    [InlineData(InvestmentAccount.ChipCashIsaAriana)]
    [InlineData(InvestmentAccount.Trading212Invested)]
    [InlineData(InvestmentAccount.ReservasPessoais)]
    public void IsLiability_ForNonLiabilityAccounts_ReturnsFalse(InvestmentAccount account)
    {
        InvestmentAccountClassification.IsLiability(account).Should().BeFalse();
    }
}
