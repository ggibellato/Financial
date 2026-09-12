using System;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Domain.Entities;

public class Transaction
{
    public enum TransactionType { Buy, Sell, Fee, Redemption, TransferIn, TransferOut, CapitalCall, ReturnOfCapital }

    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Fees { get; private set; }
    public decimal Withheld { get; private set; }

    /// <summary>
    /// The cash this transaction actually moves, keyed on its declared <see cref="CashEffect"/>
    /// rather than on <see cref="Type"/> directly: an outflow costs the gross amount plus fees and
    /// withheld tax; an inflow yields the gross amount minus fees and withheld tax; a type with no
    /// cash effect of its own (e.g. a Transfer) still moves cash equal to its fee, if any - the fee
    /// is independent of the type's own declared effect (see <see cref="TransactionTypeEffects"/>).
    /// </summary>
    public decimal NetCash => TransactionTypeEffects.For(Type).Cash switch
    {
        CashEffect.Out => -(GrossAmount + Fees + Withheld),
        CashEffect.In => GrossAmount - Fees - Withheld,
        _ => -Fees
    };

    private decimal GrossAmount => UnitPrice * Quantity;

    private Transaction() { }

    /// <summary>
    /// Fees/Withheld are floored at zero here rather than at any one call site, because a negative
    /// value is never valid on a Transaction: it would invert <see cref="NetCash"/> and carry the
    /// error into Realized Gain/Loss and the XIRR series. A caller holding a figure that might be
    /// negative - an importer recovering it from a recorded total - should report the anomaly
    /// before constructing, because from here on it is gone.
    /// <para>
    /// Deserialization does not come through here: it uses the parameterless constructor and
    /// property setters, so stored history is loaded as written rather than silently repaired.
    /// </para>
    /// </summary>
    private Transaction(Guid id, DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees, decimal withheld)
    {
        var effect = TransactionTypeEffects.For(type);
        ValidateQuantity(quantity, effect.Quantity);
        ValidateUnitPrice(unitPrice, effect.Quantity);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Date = date;
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Fees = fees < 0 ? 0 : fees;
        Withheld = withheld < 0 ? 0 : withheld;
    }

    public static Transaction Create(DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees, decimal withheld = 0m) =>
        new(Guid.NewGuid(), date, type, quantity, unitPrice, fees, withheld);

    public static Transaction CreateWithId(Guid id, DateTime date, TransactionType type, decimal quantity, decimal unitPrice, decimal fees, decimal withheld = 0m) =>
        new(id, date, type, quantity, unitPrice, fees, withheld);

    private static void ValidateQuantity(decimal quantity, QuantityEffect effect)
    {
        if (effect == QuantityEffect.None)
        {
            if (quantity != 0)
            {
                throw new ArgumentException("Quantity must be zero for a transaction type with no quantity effect.");
            }

            return;
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }
    }

    private static void ValidateUnitPrice(decimal unitPrice, QuantityEffect effect)
    {
        if (effect == QuantityEffect.None)
        {
            if (unitPrice != 0)
            {
                throw new ArgumentException("Unit price must be zero for a transaction type with no quantity effect.");
            }

            return;
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentException("Unit price must be greater than zero.");
        }
    }
}
