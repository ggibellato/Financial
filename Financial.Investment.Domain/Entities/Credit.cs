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

    public decimal NetAmount => Value - Withheld;

    private Credit() { }

    private Credit(Guid id, DateTime date, CreditType type, decimal value, decimal withheld, Currency currency, FxRateSnapshot? fxRateSnapshot)
    {
        ValidateValue(value);
        ValidateWithheld(value, withheld);

        Id = id;
        Date = date;
        Type = type;
        Value = value;
        Withheld = withheld;
        Currency = currency;
        FxRateSnapshot = fxRateSnapshot;
    }

    public static Credit Create(DateTime date, CreditType type, decimal value, decimal withheld = 0m, Currency currency = default, FxRateSnapshot? fxRateSnapshot = null) =>
        new(Guid.NewGuid(), date, type, value, withheld, currency, fxRateSnapshot);

    public static Credit CreateWithId(Guid id, DateTime date, CreditType type, decimal value, decimal withheld = 0m, Currency currency = default, FxRateSnapshot? fxRateSnapshot = null) =>
        new(id, date, type, value, withheld, currency, fxRateSnapshot);

    private static void ValidateValue(decimal value)
    {
        if (value == 0)
        {
            throw new ArgumentException("Value must not be zero.");
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
