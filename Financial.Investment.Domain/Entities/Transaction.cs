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

    private Transaction(Guid id, DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Date = date;
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Fees = fees;
    }

    public static Transaction Create(DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(Guid.NewGuid(), date, type, quantity, unitPrice, fees);

    public static Transaction CreateWithId(Guid id, DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(id, date, type, quantity, unitPrice, fees);

    /// <summary>
    /// Derives Fees as the inverse of <see cref="TotalPrice"/>, floored at zero. The derivation
    /// follows the transaction direction: for a purchase the total paid exceeds the gross amount
    /// by the fee, while for a sale the total received falls short of it by the fee.
    /// </summary>
    public static Transaction CreateFromTotal(DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal totalAmount)
    {
        var grossAmount = unitPrice * quantity;
        var fees = type == TransactionType.Buy ? totalAmount - grossAmount : grossAmount - totalAmount;
        return new(Guid.NewGuid(), date, type, quantity, unitPrice, fees < 0 ? 0 : fees);
    }
}
