using System;
using System.Text.Json.Serialization;

namespace Financial.Domain.Entities;

public class Operation
{
    public enum OperationType { Buy, Sell }

    [JsonInclude]
    public Guid Id { get; private set; }
    [JsonInclude]
    public DateTime Date { get; private set; }
    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType Type { get; private set; }
    [JsonInclude]
    public decimal Quantity { get; private set; }
    [JsonInclude]
    public decimal UnitPrice { get; private set; }
    [JsonInclude]
    public decimal Fees { get; private set; }

    [JsonIgnore]
    public decimal TotalPrice => UnitPrice * Quantity + Fees;


    [JsonConstructor]
    private Operation() { }

    private Operation(Guid id, DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Date = date;
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Fees = fees;
    }

    public static Operation Create(DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(Guid.NewGuid(), date, type, quantity, unitPrice, fees);

    public static Operation CreateWithId(Guid id, DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(id, date, type, quantity, unitPrice, fees);

    internal void EnsureId()
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
    }

}
