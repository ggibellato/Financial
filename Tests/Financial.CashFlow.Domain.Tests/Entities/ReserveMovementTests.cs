using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class ReserveMovementTests
{
    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 1);

        var movement = ReserveMovement.Create(ReserveBucket.Investimento, 866.67m, date, "Monthly income split");

        movement.Id.Should().NotBeEmpty();
        movement.Bucket.Should().Be(ReserveBucket.Investimento);
        movement.Amount.Should().Be(866.67m);
        movement.Date.Should().Be(date);
        movement.Description.Should().Be("Monthly income split");
    }

    [Fact]
    public void Create_WithNegativeAmount_RepresentsAWithdrawal()
    {
        var movement = ReserveMovement.Create(ReserveBucket.Ariana, -50m, new DateOnly(2026, 7, 1), "Withdrawal");

        movement.Amount.Should().Be(-50m);
    }

    [Fact]
    public void Create_TwoMovements_HaveDifferentIds()
    {
        var first = ReserveMovement.Create(ReserveBucket.Dizimo, 10m, new DateOnly(2026, 7, 1), "A");
        var second = ReserveMovement.Create(ReserveBucket.Dizimo, 10m, new DateOnly(2026, 7, 1), "B");

        first.Id.Should().NotBe(second.Id);
    }
}
