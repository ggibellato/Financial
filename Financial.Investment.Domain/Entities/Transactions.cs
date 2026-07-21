using System;
using System.Collections;
using System.Collections.Generic;

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

        if (transaction.Type == Transaction.TransactionType.Buy)
        {
            AveragePrice = (AveragePrice * Quantity + transaction.TotalPrice) / (Quantity + transaction.Quantity);
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

        _items.Add(transaction);
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

        EnsureNotEmptyId(updatedTransaction.Id, "Transaction Id is required for update.", nameof(updatedTransaction));

        var index = _items.FindIndex(t => t.Id == updatedTransaction.Id);
        if (index < 0)
        {
            return false;
        }

        var replayList = new List<Transaction>(_items);
        replayList[index] = updatedTransaction;
        Rebuild(replayList);
        return true;
    }

    public bool RemoveById(Guid transactionId)
    {
        EnsureNotEmptyId(transactionId, "Transaction Id is required for delete.", nameof(transactionId));

        var index = _items.FindIndex(t => t.Id == transactionId);
        if (index < 0)
        {
            return false;
        }

        var replayList = new List<Transaction>(_items);
        replayList.RemoveAt(index);
        Rebuild(replayList);
        return true;
    }

    private void Rebuild(IEnumerable<Transaction> transactions)
    {
        var replayList = new List<Transaction>(transactions);
        _items.Clear();
        Quantity = 0;
        AveragePrice = 0;
        RealizedCapitalGain = 0;
        _totalSoldQuantity = 0;
        _totalSoldValue = 0;
        foreach (var transaction in replayList)
        {
            Add(transaction);
        }
    }

    private static void EnsureNotEmptyId(Guid id, string message, string paramName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public IEnumerator<Transaction> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool ICollection<Transaction>.IsReadOnly => false;
    void ICollection<Transaction>.Clear() => Rebuild([]);
    bool ICollection<Transaction>.Contains(Transaction item) => item != null && _items.Contains(item);
    void ICollection<Transaction>.CopyTo(Transaction[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    bool ICollection<Transaction>.Remove(Transaction item) => item != null && RemoveById(item.Id);
}
