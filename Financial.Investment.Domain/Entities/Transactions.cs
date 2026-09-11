using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Financial.Investment.Domain.Entities;

/// <summary>
/// Figures are derived by replaying transactions in date order, purchases before sales on a shared
/// date (FR-001..FR-003). <see cref="Add"/> is the population entry point System.Text.Json calls once
/// per stored transaction on load, so it must stay tolerant of arriving out of order: it applies
/// incrementally while the incoming transaction still sorts last, and falls back to a full replay only
/// when it does not — <c>Add</c> is therefore not O(1)-guaranteed (worst case O(n log n) on an
/// out-of-order arrival). <see cref="Update"/> and <see cref="RemoveById"/> always replay, since an
/// edited date can reorder the whole sequence.
/// </summary>
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

        if (_items.Count == 0 || CompareReplayOrder(_items[^1], transaction) <= 0)
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

    /// <summary>
    /// Sorts the whole current set by replay order and folds it from scratch, writing the ordered
    /// sequence back into <see cref="_items"/> so storage order and figures never disagree (R11).
    /// <c>OrderBy</c>/<c>ThenBy</c> are documented-stable, so a tie keeps its arrival order — the only
    /// tiebreaker FR-003 needs, since weighted-average folding over buys is commutative and sells do
    /// not move the average.
    /// </summary>
    private void Recompute()
    {
        var ordered = _items
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Type == Transaction.TransactionType.Sell)
            .ToList();

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
    /// Folds one transaction, already in replay order, into the running totals. The Buy branch guards
    /// a zero resulting quantity (reachable only after a stored oversell leaves <see cref="Quantity"/>
    /// negative) rather than dividing by zero, resetting <see cref="AveragePrice"/> to 0 — the same
    /// value a flat, never-opened position reports. The guard is deliberately Buy-only: generalizing it
    /// to the ordinary sell-to-flat path would zero the average price of every closed historic holding
    /// (R14).
    /// </summary>
    private void Apply(Transaction transaction)
    {
        if (transaction.Type == Transaction.TransactionType.Buy)
        {
            var resultingQuantity = Quantity + transaction.Quantity;
            AveragePrice = resultingQuantity == 0
                ? 0m
                : (AveragePrice * Quantity + transaction.TotalPrice) / resultingQuantity;
        }
        else
        {
            RealizedCapitalGain += transaction.TotalPrice - (transaction.Quantity * AveragePrice);
            _totalSoldValue += transaction.TotalPrice;
            _totalSoldQuantity += transaction.Quantity;
        }

        Quantity += transaction.Type == Transaction.TransactionType.Buy
            ? transaction.Quantity
            : -transaction.Quantity;
    }

    /// <summary>Date ascending, purchases before sales on a shared date (FR-002). No tertiary tiebreaker.</summary>
    private static int CompareReplayOrder(Transaction a, Transaction b)
    {
        var byDate = a.Date.CompareTo(b.Date);
        if (byDate != 0)
        {
            return byDate;
        }

        var aIsSell = a.Type == Transaction.TransactionType.Sell;
        var bIsSell = b.Type == Transaction.TransactionType.Sell;
        return aIsSell.CompareTo(bIsSell);
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
