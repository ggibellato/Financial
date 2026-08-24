using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

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
    public void Create_TwoMovements_HaveDifferentIds()
    {
        var first = ReserveMovement.Create(Investimento, 10m, new DateOnly(2026, 7, 1), "A");
        var second = ReserveMovement.Create(Investimento, 10m, new DateOnly(2026, 7, 1), "B");

        first.Id.Should().NotBe(second.Id);
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
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, autoSplitToReserve: true), 3200m, 2450m, null, splitToReserve: true);

        var movement = ReserveMovement.Create(Investimento, 815.05m, new DateOnly(2026, 7, 1), "August salary", income);

        movement.Income.Should().BeSameAs(income);
    }

    [Fact]
    public void Update_LeavesIncomeUntouched()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, autoSplitToReserve: true), 3200m, 2450m, null, splitToReserve: true);
        var movement = ReserveMovement.Create(Investimento, 815.05m, new DateOnly(2026, 7, 1), "August salary", income);

        movement.Update(Ariana, 25m, new DateOnly(2026, 7, 5), "Corrected");

        movement.Income.Should().BeSameAs(income);
    }
}
