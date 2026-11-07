using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class MaeAccountTests
{
    [Fact]
    public void Find_SeveralSettlementAccounts_ReturnsTheFirstInFileOrder()
    {
        var mae = TestAccounts.Settlement("Mum", [Currency.BRL, Currency.GBP], "Mãe");
        var son = TestAccounts.Settlement("Samuel", [Currency.GBP], "Samuel");

        MaeAccount.Find([TestAccounts.Bank("Barclays"), mae, TestAccounts.Pots("Reserva"), son]).Should().BeSameAs(mae);
    }

    [Fact]
    public void Find_NoSettlementAccount_ReturnsNull()
    {
        MaeAccount.Find([TestAccounts.Bank("Barclays"), TestAccounts.CreditCard("BA Amex"), TestAccounts.Pots("Reserva")]).Should().BeNull();
    }
}
