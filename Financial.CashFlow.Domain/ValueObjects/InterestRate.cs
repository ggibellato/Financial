using System;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.ValueObjects;

public sealed record InterestRate
{
    public const decimal MinAnnualPercent = 0m;
    public const decimal MaxAnnualPercent = 100m;

    public decimal AnnualPercent { get; }
    public DateOnly EffectiveFrom { get; }

    private InterestRate(decimal annualPercent, DateOnly effectiveFrom)
    {
        AnnualPercent = annualPercent;
        EffectiveFrom = effectiveFrom;
    }

    public static InterestRate Create(decimal annualPercent, DateOnly effectiveFrom)
    {
        if (annualPercent < MinAnnualPercent || annualPercent > MaxAnnualPercent)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Interest rate must be between {MinAnnualPercent:0}% and {MaxAnnualPercent:0}%."));
        }

        if (MoneyRounding.ToCents(annualPercent) != annualPercent)
        {
            throw new ArgumentException("Interest rate can have at most 2 decimal places.");
        }

        return new(annualPercent, effectiveFrom);
    }
}
