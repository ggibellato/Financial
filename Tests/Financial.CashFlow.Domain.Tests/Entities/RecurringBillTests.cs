using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class RecurringBillTests
{
    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsStatusToUnset()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1621m);

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

    [Fact]
    public void Create_WithoutNitNumberOrMinimumWageValue_AllowsBothNull()
    {
        var bill = RecurringBill.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);

        bill.NitNumber.Should().BeNull();
        bill.MinimumWageValue.Should().BeNull();
    }

    [Fact]
    public void Create_TwoBills_HaveDifferentIds()
    {
        var first = RecurringBill.Create(1, "A", 10m, Area.UK, string.Empty, null, null);
        var second = RecurringBill.Create(1, "B", 10m, Area.UK, string.Empty, null, null);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Update_ChangesStatusAndValue()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);

        bill.Update(BillStatus.Paid, 900m);

        bill.Status.Should().Be(BillStatus.Paid);
        bill.Value.Should().Be(900m);
    }

    [Fact]
    public void ResetToUnset_SetsStatusBackToUnsetWithoutChangingValue()
    {
        var bill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, string.Empty, null, null);
        bill.Update(BillStatus.Paid, 900m);

        bill.ResetToUnset();

        bill.Status.Should().Be(BillStatus.Unset);
        bill.Value.Should().Be(900m);
    }
}
