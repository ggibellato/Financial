using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class RecurringBillTests
{
    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsStatusToUnset()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1621m);

        using (new AssertionScope())
        {
            bill.Id.Should().NotBeEmpty();
            bill.DueDay.Should().Be(10);
            bill.Description.Should().Be("INSS");
            bill.Value.Should().Be(850m);
            bill.Area.Should().Be(Area.Brasil);
            bill.Note.Should().Be("Direct debit");
            bill.NitNumber.Should().Be("12345678901");
            bill.MinimumWageValue.Should().Be(1621m);
            bill.Status.Should().Be(BillStatus.Unset);
        }
    }

    [Fact]
    public void Create_WithoutNitNumberOrMinimumWageValue_AllowsBothNull()
    {
        var bill = RecurringBill.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);

        bill.NitNumber.Should().BeNull();
        bill.MinimumWageValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Create_WithDueDayOutOfRange_Throws(int dueDay)
    {
        var act = () => RecurringBill.Create(dueDay, "INSS", 850m, Area.Brasil, string.Empty, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*Due day must be between 1 and 31*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankDescription_Throws(string description)
    {
        var act = () => RecurringBill.Create(10, description, 850m, Area.Brasil, string.Empty, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*Description is required*");
    }

    [Fact]
    public void Create_TwoBills_HaveDifferentIds()
    {
        var first = RecurringBill.Create(1, "A", 10m, Area.UK, string.Empty, null, null);
        var second = RecurringBill.Create(1, "B", 10m, Area.UK, string.Empty, null, null);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Update_ChangesEveryField()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);

        bill.Update(15, "INSS Renamed", 900m, Area.UK, "Updated note", "12345678901", 1621m, BillStatus.Paid);

        using (new AssertionScope())
        {
            bill.DueDay.Should().Be(15);
            bill.Description.Should().Be("INSS Renamed");
            bill.Value.Should().Be(900m);
            bill.Area.Should().Be(Area.UK);
            bill.Note.Should().Be("Updated note");
            bill.NitNumber.Should().Be("12345678901");
            bill.MinimumWageValue.Should().Be(1621m);
            bill.Status.Should().Be(BillStatus.Paid);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Update_WithDueDayOutOfRange_ThrowsAndLeavesPriorValuesUntouched(int dueDay)
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);

        var act = () => bill.Update(dueDay, "INSS Renamed", 900m, Area.UK, "Updated note", null, null, BillStatus.Paid);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("*Due day must be between 1 and 31*");
            bill.DueDay.Should().Be(10);
            bill.Description.Should().Be("INSS");
            bill.Status.Should().Be(BillStatus.Unset);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithBlankDescription_ThrowsAndLeavesPriorValuesUntouched(string description)
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);

        var act = () => bill.Update(15, description, 900m, Area.UK, "Updated note", null, null, BillStatus.Paid);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("*Description is required*");
            bill.DueDay.Should().Be(10);
            bill.Description.Should().Be("INSS");
        }
    }

    [Fact]
    public void ResetToUnset_SetsStatusBackToUnsetWithoutChangingValue()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        bill.Update(10, "INSS", 900m, Area.Brasil, string.Empty, null, null, BillStatus.Paid);

        bill.ResetToUnset();

        bill.Status.Should().Be(BillStatus.Unset);
        bill.Value.Should().Be(900m);
    }
}
