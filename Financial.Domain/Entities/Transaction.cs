using System;
using System.Text.Json.Serialization;

namespace Financial.Domain.Entities;

public class Transaction
{
    public enum TransactionType { Buy, Sell }

    [JsonInclude]
    public Guid Id { get; private set; }
    [JsonInclude]
    public DateTime Date { get; private set; }
    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionType Type { get; private set; }
    [JsonInclude]
    public decimal Quantity { get; private set; }
    [JsonInclude]
    public decimal UnitPrice { get; private set; }
    [JsonInclude]
    public decimal Fees { get; private set; }

    [JsonIgnore]
    public decimal TotalPrice => UnitPrice * Quantity + Fees;


    [JsonConstructor]
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
