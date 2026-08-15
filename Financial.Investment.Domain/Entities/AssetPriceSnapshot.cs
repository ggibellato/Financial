using System;

namespace Financial.Investment.Domain.Entities;

public class AssetPriceSnapshot
{
    public DateOnly Date { get; private set; }

    public decimal Price { get; private set; }

    public bool IsManual { get; private set; }

    private AssetPriceSnapshot() { }

    public static AssetPriceSnapshot Create(DateOnly date, decimal price, bool isManual)
    {
        ValidatePrice(price);
        ValidateDate(date);

        return new()
        {
            Date = date,
            Price = price,
            IsManual = isManual
        };
    }

    private static void ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.");
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
