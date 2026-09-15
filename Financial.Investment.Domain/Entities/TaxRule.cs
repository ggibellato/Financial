using System;

namespace Financial.Investment.Domain.Entities;

public sealed class TaxRule
{
    public Guid Id { get; private set; }
    public Jurisdiction Jurisdiction { get; private set; }
    public EventCategory EventCategory { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    private TaxRule() { }

    private TaxRule(
        Guid id,
        Jurisdiction jurisdiction,
        EventCategory eventCategory,
        string label,
        string description,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
        : this()
    {
        ValidateRange(effectiveFrom, effectiveTo);

        Id = id;
        Jurisdiction = jurisdiction;
        EventCategory = eventCategory;
        Label = RequireLabel(label);
        Description = description ?? string.Empty;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public static TaxRule Create(
        Jurisdiction jurisdiction,
        EventCategory eventCategory,
        string label,
        string description,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo) =>
        new(Guid.NewGuid(), jurisdiction, eventCategory, label, description, effectiveFrom, effectiveTo);

    public void Update(string label, string description, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        ValidateRange(effectiveFrom, effectiveTo);

        Label = RequireLabel(label);
        Description = description ?? string.Empty;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public bool Applies(DateOnly date) =>
        date >= EffectiveFrom && (EffectiveTo is null || date < EffectiveTo.Value);

    private static string RequireLabel(string label) =>
        string.IsNullOrWhiteSpace(label)
            ? throw new ArgumentException("A tax rule requires a label.", nameof(label))
            : label;

    private static void ValidateRange(DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (effectiveTo is not null && effectiveFrom >= effectiveTo.Value)
        {
            throw new ArgumentException(
                "EffectiveFrom must be strictly before EffectiveTo.", nameof(effectiveFrom));
        }
    }
}
