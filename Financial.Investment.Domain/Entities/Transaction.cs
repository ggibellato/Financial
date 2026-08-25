using System;

namespace Financial.Investment.Domain.Entities;

public class Transaction
{
    public enum TransactionType { Buy, Sell }

    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Fees { get; private set; }

    /// <summary>
    /// The cash moved by this transaction. A purchase costs the gross amount <em>plus</em> fees;
    /// a sale yields the gross amount <em>minus</em> fees, because the fee is deducted from what
    /// is received rather than added to what is paid.
    /// </summary>
    public decimal TotalPrice => IsPurchase ? GrossAmount + Fees : GrossAmount - Fees;

    private bool IsPurchase => Type == TransactionType.Buy;

    private decimal GrossAmount => UnitPrice * Quantity;

    private Transaction() { }

    /// <summary>
    /// Fees are floored at zero here rather than at any one call site, because a negative fee is
    /// never a valid Transaction: it would invert <see cref="TotalPrice"/> and carry the error into
    /// Realized Gain/Loss and the XIRR series. A caller holding a figure that might be negative -
    /// an importer recovering it from a recorded total - should report the anomaly before
    /// constructing, because from here on it is gone.
    /// <para>
    /// Deserialization does not come through here: it uses the parameterless constructor and
    /// property setters, so stored history is loaded as written rather than silently repaired.
    /// </para>
    /// </summary>
    private Transaction(Guid id, DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees)
    {
        ValidateQuantity(quantity);
        ValidateUnitPrice(unitPrice);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Date = date;
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Fees = fees < 0 ? 0 : fees;
    }

    public static Transaction Create(DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(Guid.NewGuid(), date, type, quantity, unitPrice, fees);

    public static Transaction CreateWithId(Guid id, DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(id, date, type, quantity, unitPrice, fees);

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }
    }

    private static void ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
        {
            throw new ArgumentException("Unit price must be greater than zero.");
        }
    }
}
