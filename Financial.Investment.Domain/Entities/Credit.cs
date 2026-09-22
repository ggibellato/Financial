using System;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Domain.Entities;

public class Credit
{
    public enum CreditType { Dividend, SecuritiesLendingIncome, JCP, Coupon }

    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public CreditType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal Withheld { get; private set; }
    public decimal IntermediationFee { get; private set; }
    public Currency Currency { get; private set; }
    public FxRateSnapshot? FxRateSnapshot { get; private set; }

    /// <summary>
    /// Shares this credit was earned on, when known. Left null rather than defaulted to the
    /// current position, since a later buy can grow the position past what actually earned the
    /// dividend - attribution must be explicit, never inferred.
    /// </summary>
    public decimal? SharesForDividend { get; private set; }

    public decimal NetAmount => Value - Withheld - IntermediationFee;

    private Credit() { }

    private Credit(Guid id, DateTime date, CreditType type, decimal value, decimal withheld, Currency currency, FxRateSnapshot? fxRateSnapshot, decimal? sharesForDividend, decimal intermediationFee)
    {
        ValidateValue(value);
        ValidateDeductions(value, withheld, intermediationFee);
        ValidateSharesForDividend(sharesForDividend);

        Id = id;
        Date = date;
        Type = type;
        Value = value;
        Withheld = withheld;
        IntermediationFee = intermediationFee;
        Currency = currency;
        FxRateSnapshot = fxRateSnapshot;
        SharesForDividend = sharesForDividend;
    }

    public static Credit Create(DateTime date, CreditType type, decimal value, decimal withheld = 0m, Currency currency = default, FxRateSnapshot? fxRateSnapshot = null, decimal? sharesForDividend = null, decimal intermediationFee = 0m) =>
        new(Guid.NewGuid(), date, type, value, withheld, currency, fxRateSnapshot, sharesForDividend, intermediationFee);

    public static Credit CreateWithId(Guid id, DateTime date, CreditType type, decimal value, decimal withheld = 0m, Currency currency = default, FxRateSnapshot? fxRateSnapshot = null, decimal? sharesForDividend = null, decimal intermediationFee = 0m) =>
        new(id, date, type, value, withheld, currency, fxRateSnapshot, sharesForDividend, intermediationFee);

    private static void ValidateValue(decimal value)
    {
        if (value == 0)
        {
            throw new ArgumentException("Value must not be zero.");
        }
    }

    private static void ValidateSharesForDividend(decimal? sharesForDividend)
    {
        if (sharesForDividend is <= 0)
        {
            throw new ArgumentException("SharesForDividend must be greater than zero when provided.");
        }
    }

    // Mutates in place, unlike a user edit through CreditService.UpdateCreditAsync, so it skips
    // that path's tax-classification churn.
    internal void BackfillSharesForDividend(decimal sharesForDividend)
    {
        ValidateSharesForDividend(sharesForDividend);
        SharesForDividend = sharesForDividend;
    }

    /// <summary>
    /// Withheld and IntermediationFee must each share Value's sign (or be zero) and never exceed
    /// it in magnitude on their own, and their combined magnitude must not exceed Value's either,
    /// so <see cref="NetAmount"/> can never land on the far side of zero from Value - a correction
    /// (negative Value) and an ordinary payment (positive Value) each keep their own sign all the
    /// way through to what the investor actually received.
    /// </summary>
    private static void ValidateDeductions(decimal value, decimal withheld, decimal intermediationFee)
    {
        ValidateDeductionMagnitude(value, withheld, nameof(Withheld));
        ValidateDeductionMagnitude(value, intermediationFee, nameof(IntermediationFee));
        ValidateDeductionMagnitude(value, withheld + intermediationFee, "Withheld and IntermediationFee combined");
    }

    private static void ValidateDeductionMagnitude(decimal value, decimal deduction, string name)
    {
        var withinMagnitude = value > 0 ? deduction >= 0 && deduction <= value : deduction <= 0 && deduction >= value;
        if (!withinMagnitude)
        {
            throw new ArgumentException($"{name} must share Value's sign and must not exceed it in magnitude.");
        }
    }
}
