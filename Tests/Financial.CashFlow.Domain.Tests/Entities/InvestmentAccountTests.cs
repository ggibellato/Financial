using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

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
    public void Create_TwoAccounts_HaveDifferentIds()
    {
        var first = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var second = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        first.Id.Should().NotBe(second.Id);
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
        var creditCard = CreditCard.Create("Platinum Visa 8003", isActive: true);

        var account = InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard);

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.CreditCard);
            account.CreditCard.Should().Be(creditCard);
        }
    }

    [Fact]
    public void Create_WithSourceReserveBucketsSumAndACard_Throws()
    {
        var creditCard = CreditCard.Create("Platinum Visa 8003", isActive: true);

        var act = () => InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.ReserveBucketsSum, creditCard);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSourceReserveBucketsSumAndNoCard_Succeeds()
    {
        var account = InvestmentAccount.Create(
            "Reservas pessoais", isActive: true, isLiability: false, InvestmentAccountSource.ReserveBucketsSum);

        account.Source.Should().Be(InvestmentAccountSource.ReserveBucketsSum);
    }

    [Fact]
    public void Update_SwitchingFromCreditCardToNone_ClearsCreditCard()
    {
        var creditCard = CreditCard.Create("Platinum Visa 8003", isActive: true);
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
    public void Update_SwitchingFromCreditCardToReserveBucketsSum_ClearsCreditCard()
    {
        var creditCard = CreditCard.Create("Platinum Visa 8003", isActive: true);
        var account = InvestmentAccount.Create(
            "PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.CreditCard, creditCard);

        account.Update("PlatinumVisa8003", isActive: true, isLiability: true, InvestmentAccountSource.ReserveBucketsSum);

        using (new AssertionScope())
        {
            account.Source.Should().Be(InvestmentAccountSource.ReserveBucketsSum);
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
}
