using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class ReserveMovementTests
{
    private static readonly ReserveBucket Investimento = ReserveBucket.Create("Investimento", 33.33m);
    private static readonly ReserveBucket Ariana = ReserveBucket.Create("Ariana", 16.67m);

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 1);

        var movement = ReserveMovement.Create(Investimento, 866.67m, date, "Monthly income split");

        using (new AssertionScope())
        {
            movement.Id.Should().NotBeEmpty();
            movement.Bucket.Should().BeSameAs(Investimento);
            movement.Amount.Should().Be(866.67m);
            movement.Date.Should().Be(date);
            movement.Description.Should().Be("Monthly income split");
        }
    }

    [Fact]
    public void Create_WithNegativeAmount_RepresentsAWithdrawal()
    {
        var movement = ReserveMovement.Create(Ariana, -50m, new DateOnly(2026, 7, 1), "Withdrawal");

        movement.Amount.Should().Be(-50m);
    }


    [Fact]
    public void Update_ChangesAllFields()
    {
        var movement = ReserveMovement.Create(Investimento, 10m, new DateOnly(2026, 7, 1), "Original");

        movement.Update(Ariana, 25m, new DateOnly(2026, 7, 5), "Corrected");

        using (new AssertionScope())
        {
            movement.Bucket.Should().BeSameAs(Ariana);
            movement.Amount.Should().Be(25m);
            movement.Date.Should().Be(new DateOnly(2026, 7, 5));
            movement.Description.Should().Be("Corrected");
        }
    }

    [Fact]
    public void Create_WithoutIncome_DefaultsToNull()
    {
        var movement = ReserveMovement.Create(Investimento, 866.67m, new DateOnly(2026, 7, 1), "Monthly income split");

        movement.Income.Should().BeNull();
    }

    [Fact]
    public void Create_WithIncome_AssignsIncome()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: TestAccounts.Pots("Reserva")), 3200m, 2450m, null, splitToReserve: true);

        var movement = ReserveMovement.Create(Investimento, 815.05m, new DateOnly(2026, 7, 1), "August salary", income);

        movement.Income.Should().BeSameAs(income);
    }

    [Fact]
    public void Update_LeavesIncomeUntouched()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: TestAccounts.Pots("Reserva")), 3200m, 2450m, null, splitToReserve: true);
        var movement = ReserveMovement.Create(Investimento, 815.05m, new DateOnly(2026, 7, 1), "August salary", income);

        movement.Update(Ariana, 25m, new DateOnly(2026, 7, 5), "Corrected");

        movement.Income.Should().BeSameAs(income);
    }

    [Fact]
    public void Bucket_DeactivatedAfterMovementCreated_MovementKeepsAValidBucketReference()
    {
        var bucket = ReserveBucket.Create("Ferias", 20m, isActive: true);
        var movement = ReserveMovement.Create(bucket, 100m, new DateOnly(2026, 7, 1), "Monthly split");

        bucket.Update(bucket.Name, bucket.SplitPercentage, isActive: false);

        using (new AssertionScope())
        {
            movement.Bucket.Should().BeSameAs(bucket);
            movement.Bucket.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public void ContributionTo_AccountThatOwnsThePot_IsTheAmount()
    {
        var reserva = Account.Create("Reserva", AccountTemplate.Pots, Currency.GBP);
        var pot = ReserveBucket.Create("Investimento", 100m);
        reserva.AddPot(pot);

        ReserveMovement.Create(pot, -40m, new DateOnly(2026, 7, 1), "Withdrawal").ContributionTo(reserva).Should().Be(-40m);
    }

    [Theory]
    [InlineData(AccountTemplate.Pots)]
    [InlineData(AccountTemplate.Bank)]
    [InlineData(AccountTemplate.CreditCard)]
    public void ContributionTo_AccountThatDoesNotOwnThePot_IsZero(AccountTemplate template)
    {
        var account = Account.Create("Other", template, Currency.GBP);

        ReserveMovement.Create(Investimento, 100m, new DateOnly(2026, 7, 1), "Split").ContributionTo(account).Should().Be(0m);
    }
}
