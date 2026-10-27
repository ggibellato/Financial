using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class ReservaAccountTests
{
    [Fact]
    public void Find_SeveralPotsAccounts_ReturnsTheFirstInFileOrder()
    {
        var reserva = TestAccounts.Pots("Reserva");
        var holiday = TestAccounts.Pots("Holiday");

        ReservaAccount.Find([TestAccounts.Bank("Barclays"), reserva, TestAccounts.CreditCard("BA Amex"), holiday]).Should().BeSameAs(reserva);
    }

    [Fact]
    public void Find_RenamedOrInactive_IsStillReserva()
    {
        var reserva = TestAccounts.Pots("Savings", isActive: false);

        ReservaAccount.Find([reserva, TestAccounts.Pots("Holiday")]).Should().BeSameAs(reserva);
    }

    [Fact]
    public void Find_NoPotsAccount_ReturnsNull()
    {
        ReservaAccount.Find([TestAccounts.Bank("Barclays"), TestAccounts.CreditCard("BA Amex")]).Should().BeNull();
    }
}
