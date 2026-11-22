using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.ValueObjects;

public sealed class SavingsTerms
{
    public const int MinPaymentDay = 1;
    public const int MaxPaymentDay = 31;
    public const string NoRatesMessage = "A savings account needs at least one rate.";
    public const string UnknownFrequencyMessage = "Choose how often interest is paid.";

    public InterestFrequency Frequency { get; }
    public int? PaymentDay { get; }
    public bool IsTaxFree { get; }
    public IReadOnlyList<InterestRate> Rates { get; }

    private SavingsTerms(InterestFrequency frequency, int? paymentDay, bool isTaxFree, IReadOnlyList<InterestRate> rates)
    {
        Frequency = frequency;
        PaymentDay = paymentDay;
        IsTaxFree = isTaxFree;
        Rates = rates;
    }

    public static SavingsTerms Create(InterestFrequency frequency, int? paymentDay, bool isTaxFree, IEnumerable<InterestRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        EnsurePaymentDayFits(frequency, paymentDay);
        var ordered = rates.OrderBy(rate => rate.EffectiveFrom).ToList();

        if (ordered.Count == 0)
        {
            throw new ArgumentException(NoRatesMessage);
        }

        if (ordered.DistinctBy(rate => rate.EffectiveFrom).Count() != ordered.Count)
        {
            throw new ArgumentException("Two rates can't take effect on the same date.");
        }

        return new(frequency, paymentDay, isTaxFree, ordered.AsReadOnly());
    }

    public InterestRate? CurrentRate(DateOnly today) => Rates.LastOrDefault(rate => rate.EffectiveFrom <= today);

    public InterestRate? UpcomingRate(DateOnly today) => Rates.FirstOrDefault(rate => rate.EffectiveFrom > today);

    private static void EnsurePaymentDayFits(InterestFrequency frequency, int? paymentDay)
    {
        if (!Enum.IsDefined(frequency))
        {
            throw new ArgumentException(UnknownFrequencyMessage);
        }

        if (frequency == InterestFrequency.Monthly && paymentDay is not (>= MinPaymentDay and <= MaxPaymentDay))
        {
            throw new ArgumentException($"Interest payment day must be between {MinPaymentDay} and {MaxPaymentDay}.");
        }

        if (frequency == InterestFrequency.Daily && paymentDay is not null)
        {
            throw new ArgumentException("Interest paid daily has no payment day.");
        }
    }
}
