using System;

namespace Financial.Investment.Domain.Entities;

public class AssetPriceSnapshot
{
    public DateOnly Date { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public PriceSource Source { get; private set; }

    public string? SourceReference { get; private set; }

    public ValuationMethod ValuationMethod { get; private set; }

    public DateTimeOffset RetrievedAt { get; private set; }

    public bool IsManual => Source == PriceSource.Manual;

    private AssetPriceSnapshot() { }

    public static AssetPriceSnapshot Create(
        DateOnly date,
        decimal price,
        ValuationMethod valuationMethod,
        PriceSource source,
        string currency,
        string? sourceReference,
        DateTimeOffset retrievedAt)
    {
        ValidatePrice(price, valuationMethod);
        ValidateDate(date);

        return new()
        {
            Date = date,
            Price = price,
            Currency = currency,
            Source = source,
            SourceReference = sourceReference,
            ValuationMethod = valuationMethod,
            RetrievedAt = retrievedAt
        };
    }

    /// <summary>
    /// A value-based/manual holding's recorded figure is its total worth, not a per-unit market
    /// price — zero is a valid, distinct value there (a holding written down to nothing), so only
    /// a negative figure is ever invalid for those two methods. Every other method keeps rejecting
    /// zero or below, since a market price can never legitimately be zero.
    /// </summary>
    private static void ValidatePrice(decimal price, ValuationMethod valuationMethod)
    {
        var allowsZero = valuationMethod is ValuationMethod.ProviderValue or ValuationMethod.Manual;
        if (allowsZero ? price < 0 : price <= 0)
        {
            throw new ArgumentException(allowsZero
                ? "Price must not be negative."
                : "Price must be greater than zero.");
        }
    }

    private static void ValidateDate(DateOnly date)
    {
        if (date > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Price date cannot be in the future.");
        }
    }
}
