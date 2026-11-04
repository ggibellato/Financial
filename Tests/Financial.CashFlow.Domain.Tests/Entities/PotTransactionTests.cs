using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class PotTransactionTests
{
    private static readonly Pot Investimento = Pot.Create("Investimento", 33.33m);
    private static readonly Pot Ariana = Pot.Create("Ariana", 16.67m);

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 1);

        var movement = PotTransaction.Create(Investimento, 866.67m, date, "Monthly income split");

        using (new AssertionScope())
        {
            movement.Id.Should().NotBeEmpty();
            movement.Pot.Should().BeSameAs(Investimento);
            movement.Amount.Should().Be(866.67m);
            movement.Date.Should().Be(date);
            movement.Description.Should().Be("Monthly income split");
        }
    }

    [Fact]
    public void Create_WithNegativeAmount_RepresentsAWithdrawal()
    {
        var movement = PotTransaction.Create(Ariana, -50m, new DateOnly(2026, 7, 1), "Withdrawal");

        movement.Amount.Should().Be(-50m);
    }


    [Fact]
    public void Update_ChangesAllFields()
    {
        var movement = PotTransaction.Create(Investimento, 10m, new DateOnly(2026, 7, 1), "Original");

        movement.Update(Ariana, 25m, new DateOnly(2026, 7, 5), "Corrected");

        using (new AssertionScope())
        {
            movement.Pot.Should().BeSameAs(Ariana);
            movement.Amount.Should().Be(25m);
            movement.Date.Should().Be(new DateOnly(2026, 7, 5));
            movement.Description.Should().Be("Corrected");
        }
    }

    [Fact]
    public void Create_WithoutIncome_DefaultsToNull()
    {
        var movement = PotTransaction.Create(Investimento, 866.67m, new DateOnly(2026, 7, 1), "Monthly income split");

        movement.Income.Should().BeNull();
    }

    private static Income SplitIncome()
    {
        var reserva = TestAccounts.Pots("Reserva");
        return Income.Create(
            new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: reserva), 3200m, 2450m, null, potsAccount: reserva);
    }

    [Fact]
    public void Create_WithIncome_AssignsIncome()
    {
        var income = SplitIncome();

        var movement = PotTransaction.Create(Investimento, 815.05m, new DateOnly(2026, 7, 1), "August salary", income);

        movement.Income.Should().BeSameAs(income);
    }

    [Fact]
    public void Update_LeavesIncomeUntouched()
    {
        var income = SplitIncome();
        var movement = PotTransaction.Create(Investimento, 815.05m, new DateOnly(2026, 7, 1), "August salary", income);

        movement.Update(Ariana, 25m, new DateOnly(2026, 7, 5), "Corrected");

        movement.Income.Should().BeSameAs(income);
    }

    [Fact]
    public void Pot_DeactivatedAfterMovementCreated_MovementKeepsAValidPotReference()
    {
        var pot = Pot.Create("Ferias", 20m, isActive: true);
        var movement = PotTransaction.Create(pot, 100m, new DateOnly(2026, 7, 1), "Monthly split");

        pot.Update(pot.Name, pot.SplitPercentage, isActive: false);

        using (new AssertionScope())
        {
            movement.Pot.Should().BeSameAs(pot);
            movement.Pot.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public void ContributionTo_AccountThatOwnsThePot_IsTheAmount()
    {
        var reserva = Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP);
        var pot = Pot.Create("Investimento", 100m);
        reserva.AddPot(pot);

        PotTransaction.Create(pot, -40m, new DateOnly(2026, 7, 1), "Withdrawal").ContributionTo(reserva).Should().Be(-40m);
    }

    [Theory]
    [InlineData(AccountTemplate.Pots)]
    [InlineData(AccountTemplate.Bank)]
    [InlineData(AccountTemplate.CreditCard)]
    public void ContributionTo_AccountThatDoesNotOwnThePot_IsZero(AccountTemplate template)
    {
        var account = Account.Create("Other", template, Currency.GBP);

        PotTransaction.Create(Investimento, 100m, new DateOnly(2026, 7, 1), "Split").ContributionTo(account).Should().Be(0m);
    }
}
