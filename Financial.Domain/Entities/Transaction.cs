using System;

namespace Financial.Domain.Entities;

public class Transaction
{
    public enum TransactionType { Buy, Sell }

    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Fees { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity + Fees;

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

    internal void EnsureId()
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
    }

}
