using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class BalanceAdjustmentTests
{
    private static BalanceAdjustment CreateValidAdjustment() =>
        BalanceAdjustment.Create(new DateOnly(2026, 7, 25), "Barclays", 2340.17m, -4.20m, "Matched against July statement");

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 25);

        var adjustment = BalanceAdjustment.Create(date, "Barclays", 2340.17m, -4.20m, "Matched against July statement");

        using (new AssertionScope())
        {
            adjustment.Id.Should().NotBeEmpty();
            adjustment.Date.Should().Be(date);
            adjustment.Bank.Should().Be("Barclays");
            adjustment.TargetBalance.Should().Be(2340.17m);
            adjustment.Delta.Should().Be(-4.20m);
            adjustment.Note.Should().Be("Matched against July statement");
        }
    }

    [Fact]
    public void Create_WithoutNote_AllowsNullNote()
    {
        var adjustment = BalanceAdjustment.Create(new DateOnly(2026, 7, 25), "Barclays", 100m, 0m, null);

        adjustment.Note.Should().BeNull();
    }

    [Fact]
    public void Create_TwoAdjustments_HaveDifferentIds()
    {
        var first = CreateValidAdjustment();
        var second = CreateValidAdjustment();

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithZeroTargetBalance_Succeeds()
    {
        var adjustment = BalanceAdjustment.Create(new DateOnly(2026, 7, 25), "Barclays", 0m, -100m, null);

        adjustment.TargetBalance.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeTargetBalance_Throws()
    {
        var act = () => BalanceAdjustment.Create(new DateOnly(2026, 7, 25), "Barclays", -1m, 0m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void UpdateDetails_MutatesFieldsWithoutChangingIdOrBank()
    {
        var adjustment = CreateValidAdjustment();
        var originalId = adjustment.Id;
        var newDate = new DateOnly(2026, 8, 1);

        adjustment.UpdateDetails(newDate, 500m, 10m, "Updated note");

        using (new AssertionScope())
        {
            adjustment.Id.Should().Be(originalId);
            adjustment.Bank.Should().Be("Barclays");
            adjustment.Date.Should().Be(newDate);
            adjustment.TargetBalance.Should().Be(500m);
            adjustment.Delta.Should().Be(10m);
            adjustment.Note.Should().Be("Updated note");
        }
    }

    [Fact]
    public void UpdateDetails_WithNegativeTargetBalance_Throws()
    {
        var adjustment = CreateValidAdjustment();

        var act = () => adjustment.UpdateDetails(adjustment.Date, -1m, 0m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }
}
