using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class RecurringBillTemplateTests
{
    [Fact]
    public void Create_AssignsAllFieldsANewIdAndDefaultsIsActiveToTrue()
    {
        var template = RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1412m);

        template.Id.Should().NotBeEmpty();
        template.DueDay.Should().Be(10);
        template.Description.Should().Be("INSS");
        template.Value.Should().Be(850m);
        template.Area.Should().Be(Area.Brasil);
        template.Note.Should().Be("Direct debit");
        template.NitNumber.Should().Be("12345678901");
        template.MinimumWageValue.Should().Be(1412m);
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutNitNumberOrMinimumWageValue_AllowsBothNull()
    {
        var template = RecurringBillTemplate.Create(15, "Council Tax", 120m, Area.UK, string.Empty, null, null);

        template.NitNumber.Should().BeNull();
        template.MinimumWageValue.Should().BeNull();
    }

    [Fact]
    public void Create_TwoTemplates_HaveDifferentIds()
    {
        var first = RecurringBillTemplate.Create(1, "A", 10m, Area.UK, string.Empty, null, null);
        var second = RecurringBillTemplate.Create(1, "B", 10m, Area.UK, string.Empty, null, null);

        first.Id.Should().NotBe(second.Id);
    }
}
