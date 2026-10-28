using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Financial.TestUtilities;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class InvestmentAccountTests
{
    [Fact]
    public void Create_WithValidName_AssignsAllFieldsAndANewId()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        using (new AssertionScope())
        {
            account.Id.Should().NotBeEmpty();
            account.Name.Should().Be("ChaseSave");
            account.IsActive.Should().BeTrue();
            account.IsLiability.Should().BeFalse();
        }
    }

    [Fact]
    public void Create_WithLiabilityFlag_SetsIsLiabilityTrue()
    {
        var account = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);

        account.IsLiability.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => InvestmentAccount.Create(name!, isActive: true, isLiability: false);

        act.Should().Throw<ArgumentException>();
    }


    [Fact]
    public void Update_ChangesNameActiveAndLiability()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        account.Update("ChaseSaveRenamed", isActive: false, isLiability: true);

        using (new AssertionScope())
        {
            account.Name.Should().Be("ChaseSaveRenamed");
            account.IsActive.Should().BeFalse();
            account.IsLiability.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesPriorValuesUntouched(string? name)
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        var act = () => account.Update(name!, isActive: false, isLiability: true);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            account.Name.Should().Be("ChaseSave");
            account.IsActive.Should().BeTrue();
            account.IsLiability.Should().BeFalse();
        }
    }

    [Fact]
    public void Create_WithSourceNone_DefaultsSucceed()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.None);
            account.CreditCard.Should().BeNull();
        }
    }

    [Fact]
    public void Create_WithSourceCreditCardAndNoCard_Throws()
    {
        var act = () => InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSourceCreditCardAndCard_Succeeds()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);

        var account = InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard);

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.CreditCard);
            account.CreditCard.Should().Be(creditCard);
        }
    }

    [Fact]
    public void Create_WithSourcePotsAccountTotalAndACard_Throws()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);

        var act = () => InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, creditCard, TestAccounts.Pots("Reserva"));

        act.Should().Throw<ArgumentException>().WithMessage("Only a credit-card-sourced investment account can have a linked credit card.");
    }

    [Fact]
    public void Create_WithSourcePotsAccountTotalAndAPotsAccount_NamesIt()
    {
        var reserva = TestAccounts.Pots("Reserva");

        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.PotsAccountTotal);
            account.PotsAccount.Should().BeSameAs(reserva);
            account.CreditCard.Should().BeNull();
        }
    }

    [Fact]
    public void Create_WithSourcePotsAccountTotalAndNoPotsAccount_Throws()
    {
        var act = () => InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal);

        act.Should().Throw<ArgumentException>().WithMessage("A pots-account-sourced investment account requires a Pots account.");
    }

    [Fact]
    public void Create_WithAPotsAccountAndAnotherSource_Throws()
    {
        var act = () => InvestmentAccount.Create(
            "ChaseSave", isActive: true, isLiability: false, InvestmentAccountSource.None, potsAccount: TestAccounts.Pots("Reserva"));

        act.Should().Throw<ArgumentException>().WithMessage("Only a pots-account-sourced investment account can name a Pots account.");
    }

    [Fact]
    public void Create_NamingAnAccountWithoutPots_Throws()
    {
        var act = () => InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: TestAccounts.Bank("Barclays"));

        act.Should().Throw<ArgumentException>().WithMessage("Barclays is not a Pots account.");
    }

    [Fact]
    public void Create_NamingAnInactivePotsAccount_Throws()
    {
        var act = () => InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal,
            potsAccount: TestAccounts.Pots("Reserva", isActive: false));

        act.Should().Throw<ArgumentException>().WithMessage("Reserva is inactive.");
    }

    [Fact]
    public void Update_KeepingAPotsAccountThatBecameInactive_Succeeds()
    {
        var reserva = TestAccounts.Pots("Reserva");
        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);
        reserva.SetActive(false);

        account.Update("Reservas", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);

        using (new AssertionScope())
        {
            account.Name.Should().Be("Reservas");
            account.PotsAccount.Should().BeSameAs(reserva);
        }
    }

    [Fact]
    public void Update_ChangingToAnInactivePotsAccount_Throws()
    {
        var reserva = TestAccounts.Pots("Reserva");
        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: reserva);

        var act = () => account.Update(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal,
            potsAccount: TestAccounts.Pots("Holidays", isActive: false));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Holidays is inactive.");
            account.PotsAccount.Should().BeSameAs(reserva);
        }
    }

    [Fact]
    public void Update_SwitchingAwayFromPotsAccountTotal_ClearsThePotsAccount()
    {
        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.PotsAccountTotal, potsAccount: TestAccounts.Pots("Reserva"));

        account.Update("Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.None);

        account.PotsAccount.Should().BeNull();
    }

    [Fact]
    public void Update_SwitchingFromCreditCardToNone_ClearsCreditCard()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);
        var account = InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard);

        account.Update("PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.None);

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.None);
            account.CreditCard.Should().BeNull();
        }
    }

    [Fact]
    public void Update_SwitchingFromCreditCardToPotsAccountTotal_ClearsCreditCard()
    {
        var creditCard = TestAccounts.CreditCard("Platinum Visa 8003", isActive: true);
        var account = InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard);

        account.Update(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.PotsAccountTotal, potsAccount: TestAccounts.Pots("Reserva"));

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.PotsAccountTotal);
            account.CreditCard.Should().BeNull();
        }
    }

    [Fact]
    public void Update_WithSourceCreditCardAndNoCard_Throws()
    {
        var account = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);

        var act = () => account.Update("PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_CreditCardSourceLinkedToABank_Throws()
    {
        var act = () => InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, TestAccounts.Bank("Chase"));

        act.Should().Throw<ArgumentException>().WithMessage("Chase is not a card account.");
    }
}
