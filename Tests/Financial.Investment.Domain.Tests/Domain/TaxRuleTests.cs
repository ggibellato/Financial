using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

public class TaxRuleTests
{
    private static TaxRule CreateRule(
        Jurisdiction jurisdiction = Jurisdiction.BR,
        EventCategory eventCategory = EventCategory.Dividend,
        string label = "BR dividend withholding — 2026 change",
        string description = "Effective 2026-01-01.",
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveTo = null) =>
        TaxRule.Create(
            jurisdiction,
            eventCategory,
            label,
            description,
            effectiveFrom ?? new DateOnly(2026, 1, 1),
            effectiveTo);

    [Fact]
    public void Create_SetsProperties()
    {
        var rule = CreateRule(effectiveTo: new DateOnly(2027, 1, 1));

        using (new AssertionScope())
        {
            rule.Id.Should().NotBeEmpty();
            rule.Jurisdiction.Should().Be(Jurisdiction.BR);
            rule.EventCategory.Should().Be(EventCategory.Dividend);
            rule.Label.Should().Be("BR dividend withholding — 2026 change");
            rule.Description.Should().Be("Effective 2026-01-01.");
            rule.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
            rule.EffectiveTo.Should().Be(new DateOnly(2027, 1, 1));
        }
    }

    [Fact]
    public void Create_WithNoEffectiveTo_IsOpenEnded()
    {
        var rule = CreateRule(effectiveTo: null);

        rule.EffectiveTo.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLabel_Throws(string? label)
    {
        var act = () => CreateRule(label: label!);

        act.Should().Throw<ArgumentException>().WithParameterName("label");
    }

    [Fact]
    public void Create_EffectiveFromOnOrAfterEffectiveTo_Throws()
    {
        var act = () => CreateRule(effectiveFrom: new DateOnly(2026, 1, 1), effectiveTo: new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentException>().WithParameterName("effectiveFrom");
    }

    [Fact]
    public void Create_EffectiveFromAfterEffectiveTo_Throws()
    {
        var act = () => CreateRule(effectiveFrom: new DateOnly(2026, 6, 1), effectiveTo: new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentException>().WithParameterName("effectiveFrom");
    }

    [Fact]
    public void Update_ChangesLabelDescriptionAndRange()
    {
        var rule = CreateRule();

        rule.Update("New label", "New description", new DateOnly(2027, 1, 1), new DateOnly(2028, 1, 1));

        using (new AssertionScope())
        {
            rule.Label.Should().Be("New label");
            rule.Description.Should().Be("New description");
            rule.EffectiveFrom.Should().Be(new DateOnly(2027, 1, 1));
            rule.EffectiveTo.Should().Be(new DateOnly(2028, 1, 1));
        }
    }

    [Fact]
    public void Update_WithBlankLabel_ThrowsAndLeavesPriorValuesUntouched()
    {
        var rule = CreateRule();

        var act = () => rule.Update("", "New description", new DateOnly(2027, 1, 1), null);

        act.Should().Throw<ArgumentException>();
        rule.Label.Should().Be("BR dividend withholding — 2026 change");
    }

    [Fact]
    public void Update_EffectiveFromOnOrAfterEffectiveTo_ThrowsAndLeavesPriorValuesUntouched()
    {
        var rule = CreateRule();

        var act = () => rule.Update("New label", "New description", new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 1));

        act.Should().Throw<ArgumentException>();
        rule.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void Applies_BeforeEffectiveFrom_IsFalse()
    {
        var rule = CreateRule(effectiveFrom: new DateOnly(2026, 1, 1), effectiveTo: new DateOnly(2027, 1, 1));

        rule.Applies(new DateOnly(2025, 12, 31)).Should().BeFalse();
    }

    [Fact]
    public void Applies_OnEffectiveFrom_IsTrue()
    {
        var rule = CreateRule(effectiveFrom: new DateOnly(2026, 1, 1), effectiveTo: new DateOnly(2027, 1, 1));

        rule.Applies(new DateOnly(2026, 1, 1)).Should().BeTrue();
    }

    [Fact]
    public void Applies_OnOrAfterEffectiveTo_IsFalse()
    {
        var rule = CreateRule(effectiveFrom: new DateOnly(2026, 1, 1), effectiveTo: new DateOnly(2027, 1, 1));

        using (new AssertionScope())
        {
            rule.Applies(new DateOnly(2027, 1, 1)).Should().BeFalse();
            rule.Applies(new DateOnly(2027, 6, 1)).Should().BeFalse();
        }
    }

    [Fact]
    public void Applies_WithNoEffectiveTo_IsTrueIndefinitely()
    {
        var rule = CreateRule(effectiveFrom: new DateOnly(2026, 1, 1), effectiveTo: null);

        rule.Applies(new DateOnly(2099, 1, 1)).Should().BeTrue();
    }
}
