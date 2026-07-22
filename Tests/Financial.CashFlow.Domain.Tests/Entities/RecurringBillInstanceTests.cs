using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class RecurringBillInstanceTests
{
    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsStatusToUnset()
    {
        var templateId = Guid.NewGuid();

        var instance = RecurringBillInstance.Create(templateId, 2026, 7, 850m);

        instance.Id.Should().NotBeEmpty();
        instance.TemplateId.Should().Be(templateId);
        instance.Year.Should().Be(2026);
        instance.Month.Should().Be(7);
        instance.Value.Should().Be(850m);
        instance.Status.Should().Be(BillStatus.Unset);
    }

    [Fact]
    public void Update_ChangesStatusAndValueWithoutChangingIdentityFields()
    {
        var templateId = Guid.NewGuid();
        var instance = RecurringBillInstance.Create(templateId, 2026, 7, 850m);
        var originalId = instance.Id;

        instance.Update(BillStatus.Paid, 900m);

        instance.Id.Should().Be(originalId);
        instance.TemplateId.Should().Be(templateId);
        instance.Year.Should().Be(2026);
        instance.Month.Should().Be(7);
        instance.Status.Should().Be(BillStatus.Paid);
        instance.Value.Should().Be(900m);
    }

    [Fact]
    public void Create_TwoInstances_HaveDifferentIds()
    {
        var first = RecurringBillInstance.Create(Guid.NewGuid(), 2026, 7, 100m);
        var second = RecurringBillInstance.Create(Guid.NewGuid(), 2026, 7, 100m);

        first.Id.Should().NotBe(second.Id);
    }
}
