using System;
using System.Collections;
using System.Collections.Generic;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Domain.Entities;

public class Transactions : ICollection<Transaction>
{
    private readonly List<Transaction> _items = new();
    private decimal _totalSoldQuantity;
    private decimal _totalSoldValue;

    public decimal Quantity { get; private set; }
    public decimal AveragePrice { get; private set; }

    /// <summary>Pure capital gain/loss from closed (sold) quantity. Does NOT include Credits.</summary>
    public decimal RealizedCapitalGain { get; private set; }

    public decimal? AverageSellPrice => _totalSoldQuantity == 0 ? null : _totalSoldValue / _totalSoldQuantity;

    public int Count => _items.Count;

    public void Add(Transaction transaction)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (_items.Count == 0 || TransactionReplayOrder.IsInOrder(_items[^1], transaction))
        {
            _items.Add(transaction);
            Apply(transaction);
        }
        else
        {
            _items.Add(transaction);
            Recompute();
        }
    }

    public void AddRange(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            Add(transaction);
        }
    }

    public bool Update(Transaction updatedTransaction)
    {
        if (updatedTransaction == null)
        {
            throw new ArgumentNullException(nameof(updatedTransaction));
        }

        EntityGuard.EnsureNotEmptyId(updatedTransaction.Id, "Transaction Id is required for update.", nameof(updatedTransaction));

        var index = _items.FindIndex(t => t.Id == updatedTransaction.Id);
        if (index < 0)
        {
            return false;
        }

        _items[index] = updatedTransaction;
        Recompute();
        return true;
    }

    public bool RemoveById(Guid transactionId)
    {
        EntityGuard.EnsureNotEmptyId(transactionId, "Transaction Id is required for delete.", nameof(transactionId));

        var index = _items.FindIndex(t => t.Id == transactionId);
        if (index < 0)
        {
            return false;
        }

        _items.RemoveAt(index);
        Recompute();
        return true;
    }

    private void Recompute()
    {
        var ordered = new List<Transaction>(TransactionReplayOrder.Sort(_items));

        _items.Clear();
        Quantity = 0;
        AveragePrice = 0;
        RealizedCapitalGain = 0;
        _totalSoldQuantity = 0;
        _totalSoldValue = 0;

        foreach (var transaction in ordered)
        {
            _items.Add(transaction);
            Apply(transaction);
        }
    }

    /// <summary>
    /// Generalizes Buy/Sell's original two-way split to every <see cref="QuantityEffect"/>: an
    /// Increase (Buy, TransferIn) feeds <see cref="AveragePrice"/>; a Decrease with a real cash
    /// effect (Sell, Redemption) realizes gain/loss against its proceeds; a Decrease with no cash
    /// effect (TransferOut) realizes zero gain/loss, since no consideration changed hands - it
    /// reduces quantity at the existing average price instead. A None-quantity-effect type (Fee,
    /// CapitalCall, ReturnOfCapital) is a pure cash entry with no effect here at all.
    /// </summary>
    private void Apply(Transaction transaction)
    {
        var effect = TransactionTypeEffects.For(transaction.Type);

        switch (effect.Quantity)
        {
            case QuantityEffect.Increase:
                // Zero-guard is Increase-only: applying it to an ordinary decrease-to-flat would
                // zero the average price of every closed historic holding instead of just an
                // oversell recovery.
                var resultingQuantity = Quantity + transaction.Quantity;
                var cost = transaction.UnitPrice * transaction.Quantity + transaction.Fees;
                AveragePrice = resultingQuantity == 0
                    ? 0m
                    : (AveragePrice * Quantity + cost) / resultingQuantity;
                Quantity = resultingQuantity;
                break;

            case QuantityEffect.Decrease:
                if (effect.Cash != CashEffect.None)
                {
                    var proceeds = transaction.NetCash;
                    RealizedCapitalGain += proceeds - (transaction.Quantity * AveragePrice);
                    _totalSoldValue += proceeds;
                    _totalSoldQuantity += transaction.Quantity;
                }

                Quantity -= transaction.Quantity;
                break;
        }
    }

    public IEnumerator<Transaction> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool ICollection<Transaction>.IsReadOnly => false;
    void ICollection<Transaction>.Clear()
    {
        _items.Clear();
        Recompute();
    }
    bool ICollection<Transaction>.Contains(Transaction item) => item != null && _items.Contains(item);
    void ICollection<Transaction>.CopyTo(Transaction[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    bool ICollection<Transaction>.Remove(Transaction item) => item != null && RemoveById(item.Id);
}
