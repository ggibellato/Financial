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
    public Currency Currency { get; private set; }
    public FxRateSnapshot? FxRateSnapshot { get; private set; }

    /// <summary>
    /// Shares this credit was earned on, when known. Left null rather than defaulted to the
    /// current position, since a later buy can grow the position past what actually earned the
    /// dividend - attribution must be explicit, never inferred.
    /// </summary>
    public decimal? SharesForDividend { get; private set; }

    public decimal NetAmount => Value - Withheld;

    private Credit() { }

    private Credit(Guid id, DateTime date, CreditType type, decimal value, decimal withheld, Currency currency, FxRateSnapshot? fxRateSnapshot, decimal? sharesForDividend)
    {
        ValidateValue(value);
        ValidateWithheld(value, withheld);
        ValidateSharesForDividend(sharesForDividend);

        Id = id;
        Date = date;
        Type = type;
        Value = value;
        Withheld = withheld;
        Currency = currency;
        FxRateSnapshot = fxRateSnapshot;
        SharesForDividend = sharesForDividend;
    }

    public static Credit Create(DateTime date, CreditType type, decimal value, decimal withheld = 0m, Currency currency = default, FxRateSnapshot? fxRateSnapshot = null, decimal? sharesForDividend = null) =>
        new(Guid.NewGuid(), date, type, value, withheld, currency, fxRateSnapshot, sharesForDividend);

    public static Credit CreateWithId(Guid id, DateTime date, CreditType type, decimal value, decimal withheld = 0m, Currency currency = default, FxRateSnapshot? fxRateSnapshot = null, decimal? sharesForDividend = null) =>
        new(id, date, type, value, withheld, currency, fxRateSnapshot, sharesForDividend);

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

    /// <summary>
    /// Withheld must share Value's sign (or be zero) and never exceed it in magnitude, so
    /// <see cref="NetAmount"/> can never land on the far side of zero from Value - a correction
    /// (negative Value) and an ordinary payment (positive Value) each keep their own sign all the
    /// way through to what the investor actually received.
    /// </summary>
    private static void ValidateWithheld(decimal value, decimal withheld)
    {
        var withinMagnitude = value > 0 ? withheld >= 0 && withheld <= value : withheld <= 0 && withheld >= value;
        if (!withinMagnitude)
        {
            throw new ArgumentException("Withheld must share Value's sign and must not exceed it in magnitude.");
        }
    }
}
