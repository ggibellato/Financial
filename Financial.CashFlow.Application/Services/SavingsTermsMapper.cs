using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Validation;

namespace Financial.CashFlow.Application.Services;

internal static class SavingsTermsMapper
{
    internal static SavingsTerms? ToDomain(SavingsTermsDTO? dto)
    {
        if (dto is null)
        {
            return null;
        }

        if (!EnumParser.TryParseEnum<InterestFrequency>(dto.InterestFrequency, out var frequency))
        {
            throw new ArgumentException(SavingsTerms.UnknownFrequencyMessage);
        }

        return SavingsTerms.Create(
            frequency,
            dto.PaymentDay,
            dto.IsTaxFree,
            (dto.Rates ?? []).Select(rate => InterestRate.Create(rate.AnnualPercent, rate.EffectiveFrom)));
    }

    internal static SavingsTermsDTO? ToDto(SavingsTerms? terms) => terms is null
        ? null
        : new SavingsTermsDTO
        {
            InterestFrequency = terms.Frequency.ToString(),
            PaymentDay = terms.PaymentDay,
            IsTaxFree = terms.IsTaxFree,
            Rates = terms.Rates.Select(ToDto).ToList()
        };

    internal static SavingsSummaryDTO? ToSummary(SavingsTerms? terms, DateOnly today) => terms is null
        ? null
        : new SavingsSummaryDTO
        {
            InterestFrequency = terms.Frequency.ToString(),
            PaymentDay = terms.PaymentDay,
            IsTaxFree = terms.IsTaxFree,
            CurrentRate = terms.CurrentRate(today) is { } current ? ToDto(current) : null,
            UpcomingRate = terms.UpcomingRate(today) is { } upcoming ? ToDto(upcoming) : null
        };

    private static InterestRateDTO ToDto(InterestRate rate) =>
        new() { AnnualPercent = rate.AnnualPercent, EffectiveFrom = rate.EffectiveFrom };
}
